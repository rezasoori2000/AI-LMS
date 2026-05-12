using LMS.Application.Common.Interfaces;
using LMS.Domain.Catalog;
using LMS.Domain.Conversations;
using LMS.Domain.Curriculum;
using LMS.Domain.Enrollments;
using LMS.Domain.Progress;
using LMS.Domain.Students;
using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Persistence;

/// <summary>
/// Seeds a minimal, deterministic dataset into the database for local development and demo use.
///
/// Strategy:
/// - Idempotent: each section checks for the presence of a sentinel record before inserting.
///   Re-running seed is always safe; no duplicates will be created.
/// - Fixed GUIDs: seed entities use hard-coded GUIDs so related records remain stable across
///   seed runs and across developer machines.
/// - Development-only: the API startup only calls this in the Development environment.
///   Staging and production never touch this class.
/// - Minimal content: enough to drive every Phase 1 feature (browse lessons, enroll,
///   track progress, AI chat) but not so much that it becomes a maintenance burden.
///
/// What is seeded:
///   Users        — 1 SuperAdmin, 1 TenantAdmin, 1 ContentEditor, 1 Teacher, 2 Parents, 2 Students
///   Grades       — Grade 5, Grade 6, Grade 7
///   Subjects     — Mathematics, English Language Arts
///   Chapters     — 2 chapters per subject per grade (4 total; Grade 5 only for brevity)
///   Lessons      — 2 lessons per chapter (8 total)
///   Questions    — 2 questions per lesson (16 total)
///   Profiles     — ParentProfile + StudentProfile for each parent/student user
///   Enrollments  — Student A enrolled in both subjects
///   Progress     — Student A has started Lesson 1 (InProgress) and completed Lesson 2
///
/// What is NOT seeded (deferred):
///   - AI conversations (created by users at runtime)
///   - Tenant records (Phase 3)
///   - Teacher assignments (Phase 3)
///   - Assessment results / lesson attempts (Phase 3)
/// </summary>
public sealed class DatabaseSeeder
{
    // ── Fixed sentinel / seed GUIDs ──────────────────────────────────────────

    // Users
    private static readonly Guid SuperAdminId    = Guid.Parse("00000001-0000-0000-0000-000000000001");
    private static readonly Guid TenantAdminId   = Guid.Parse("00000001-0000-0000-0000-000000000002");
    private static readonly Guid ContentEditorId = Guid.Parse("00000001-0000-0000-0000-000000000003");
    private static readonly Guid TeacherId       = Guid.Parse("00000001-0000-0000-0000-000000000004");
    private static readonly Guid ParentAId       = Guid.Parse("00000001-0000-0000-0000-000000000005");
    private static readonly Guid ParentBId       = Guid.Parse("00000001-0000-0000-0000-000000000006");
    private static readonly Guid StudentAId      = Guid.Parse("00000001-0000-0000-0000-000000000007");
    private static readonly Guid StudentBId      = Guid.Parse("00000001-0000-0000-0000-000000000008");

    // Grades
    private static readonly Guid Grade5Id = Guid.Parse("00000002-0000-0000-0000-000000000001");
    private static readonly Guid Grade6Id = Guid.Parse("00000002-0000-0000-0000-000000000002");
    private static readonly Guid Grade7Id = Guid.Parse("00000002-0000-0000-0000-000000000003");

    // Subjects
    private static readonly Guid MathId    = Guid.Parse("00000003-0000-0000-0000-000000000001");
    private static readonly Guid EnglishId = Guid.Parse("00000003-0000-0000-0000-000000000002");

    // Chapters (Grade 5 only for initial seed)
    private static readonly Guid MathG5Ch1Id    = Guid.Parse("00000004-0000-0000-0000-000000000001");
    private static readonly Guid MathG5Ch2Id    = Guid.Parse("00000004-0000-0000-0000-000000000002");
    private static readonly Guid EnglishG5Ch1Id = Guid.Parse("00000004-0000-0000-0000-000000000003");
    private static readonly Guid EnglishG5Ch2Id = Guid.Parse("00000004-0000-0000-0000-000000000004");

    // Lessons
    private static readonly Guid MathL1Id = Guid.Parse("00000005-0000-0000-0000-000000000001");
    private static readonly Guid MathL2Id = Guid.Parse("00000005-0000-0000-0000-000000000002");
    private static readonly Guid MathL3Id = Guid.Parse("00000005-0000-0000-0000-000000000003");
    private static readonly Guid MathL4Id = Guid.Parse("00000005-0000-0000-0000-000000000004");
    private static readonly Guid EngL1Id  = Guid.Parse("00000005-0000-0000-0000-000000000005");
    private static readonly Guid EngL2Id  = Guid.Parse("00000005-0000-0000-0000-000000000006");
    private static readonly Guid EngL3Id  = Guid.Parse("00000005-0000-0000-0000-000000000007");
    private static readonly Guid EngL4Id  = Guid.Parse("00000005-0000-0000-0000-000000000008");

    // Profiles
    private static readonly Guid ParentProfileAId  = Guid.Parse("00000006-0000-0000-0000-000000000001");
    private static readonly Guid ParentProfileBId  = Guid.Parse("00000006-0000-0000-0000-000000000002");
    private static readonly Guid StudentProfileAId = Guid.Parse("00000006-0000-0000-0000-000000000003");
    private static readonly Guid StudentProfileBId = Guid.Parse("00000006-0000-0000-0000-000000000004");

    // ── Dependencies ─────────────────────────────────────────────────────────

    private readonly LmsDbContext    _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        LmsDbContext           db,
        IPasswordHasher        hasher,
        ILogger<DatabaseSeeder> logger)
    {
        _db     = db;
        _hasher = hasher;
        _logger = logger;
    }

    /// <summary>
    /// Runs all seed sections.  Each section is idempotent.
    /// Migrations are applied first so the schema is always current before seeding.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Applying pending migrations…");
        await _db.Database.MigrateAsync(ct);

        _logger.LogInformation("Seeding development data…");

        await SeedGradesAsync(ct);
        await SeedSubjectsAsync(ct);
        await SeedUsersAsync(ct);
        await SeedChaptersAsync(ct);
        await SeedLessonsAsync(ct);
        await SeedQuestionsAsync(ct);
        await SeedProfilesAsync(ct);
        await SeedEnrollmentsAsync(ct);
        await SeedProgressAsync(ct);

        _logger.LogInformation("Seed complete.");
    }

    // ── Grades ────────────────────────────────────────────────────────────────

    private async Task SeedGradesAsync(CancellationToken ct)
    {
        if (await _db.Grades.AnyAsync(ct)) return;

        _db.Grades.AddRange(
            FixedGrade(Grade5Id, "Grade 5", 5),
            FixedGrade(Grade6Id, "Grade 6", 6),
            FixedGrade(Grade7Id, "Grade 7", 7));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 3 grades.");
    }

    // ── Subjects ──────────────────────────────────────────────────────────────

    private async Task SeedSubjectsAsync(CancellationToken ct)
    {
        if (await _db.Subjects.AnyAsync(ct)) return;

        _db.Subjects.AddRange(
            FixedSubject(MathId,    "Mathematics",        "mathematics",
                         "Numbers, algebra, geometry and data handling."),
            FixedSubject(EnglishId, "English Language Arts", "english",
                         "Reading, writing, grammar and comprehension."));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 2 subjects.");
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(ct)) return;

        // All seed accounts share the same dev password — never use in staging/production.
        const string devPassword = "Seed@1234!";

        _db.Users.AddRange(
            FixedUser(SuperAdminId,    "superadmin@lms.dev",   devPassword, UserRole.SuperAdmin,    "Super",   "Admin"),
            FixedUser(TenantAdminId,   "admin@lms.dev",        devPassword, UserRole.TenantAdmin,   "Tenant",  "Admin"),
            FixedUser(ContentEditorId, "editor@lms.dev",       devPassword, UserRole.ContentEditor, "Content", "Editor"),
            FixedUser(TeacherId,       "teacher@lms.dev",      devPassword, UserRole.Teacher,       "Sam",     "Teacher"),
            FixedUser(ParentAId,       "parent.a@lms.dev",     devPassword, UserRole.Parent,        "Alice",   "Parent"),
            FixedUser(ParentBId,       "parent.b@lms.dev",     devPassword, UserRole.Parent,        "Bob",     "Parent"),
            FixedUser(StudentAId,      "student.a@lms.dev",    devPassword, UserRole.Student,       "Anna",    "Student"),
            FixedUser(StudentBId,      "student.b@lms.dev",    devPassword, UserRole.Student,       "Ben",     "Student"));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 8 users (password: {Password}).", devPassword);
    }

    // ── Chapters ──────────────────────────────────────────────────────────────

    private async Task SeedChaptersAsync(CancellationToken ct)
    {
        if (await _db.Chapters.AnyAsync(ct)) return;

        _db.Chapters.AddRange(
            // Mathematics — Grade 5
            FixedChapter(MathG5Ch1Id,    MathId, Grade5Id, "Numbers and Place Value",  1),
            FixedChapter(MathG5Ch2Id,    MathId, Grade5Id, "Addition and Subtraction", 2),
            // English — Grade 5
            FixedChapter(EnglishG5Ch1Id, EnglishId, Grade5Id, "Reading Comprehension", 1),
            FixedChapter(EnglishG5Ch2Id, EnglishId, Grade5Id, "Writing Skills",        2));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 4 chapters.");
    }

    // ── Lessons ───────────────────────────────────────────────────────────────

    private async Task SeedLessonsAsync(CancellationToken ct)
    {
        if (await _db.Lessons.AnyAsync(ct)) return;

        _db.Lessons.AddRange(
            // Math Ch1: Numbers and Place Value
            FixedLesson(MathL1Id, MathG5Ch1Id, "Understanding Place Value",   1, 20,
                "## Place Value\n\nEach digit in a number has a **place value**.\n\n" +
                "For example, in the number **4,325**:\n- 4 is in the thousands place\n" +
                "- 3 is in the hundreds place\n- 2 is in the tens place\n- 5 is in the ones place.\n\n" +
                "Understanding place value helps us read, write and compare numbers."),

            FixedLesson(MathL2Id, MathG5Ch1Id, "Comparing and Ordering Numbers", 2, 20,
                "## Comparing Numbers\n\nWe use the symbols **>** (greater than), **<** (less than) " +
                "and **=** (equal to) to compare numbers.\n\n" +
                "Compare from left to right — the first digit that differs tells you which number is larger."),

            // Math Ch2: Addition and Subtraction
            FixedLesson(MathL3Id, MathG5Ch2Id, "Column Addition",    1, 25,
                "## Column Addition\n\nAlign the digits by place value, then add from right to left. " +
                "Carry over any value larger than 9 to the next column."),

            FixedLesson(MathL4Id, MathG5Ch2Id, "Column Subtraction", 2, 25,
                "## Column Subtraction\n\nAlign the digits by place value. " +
                "Subtract from right to left, borrowing from the next column when needed."),

            // English Ch1: Reading Comprehension
            FixedLesson(EngL1Id, EnglishG5Ch1Id, "Finding the Main Idea", 1, 20,
                "## Main Idea\n\nThe **main idea** is the most important point the author is making. " +
                "It is often stated in the first or last sentence of a paragraph."),

            FixedLesson(EngL2Id, EnglishG5Ch1Id, "Making Inferences",    2, 20,
                "## Making Inferences\n\nAn inference is a conclusion you draw from clues in the text " +
                "combined with what you already know."),

            // English Ch2: Writing Skills
            FixedLesson(EngL3Id, EnglishG5Ch2Id, "Paragraph Structure",  1, 25,
                "## Paragraph Structure\n\nA well-structured paragraph has three parts:\n" +
                "1. **Topic sentence** — states the main idea\n" +
                "2. **Supporting sentences** — provide details and examples\n" +
                "3. **Concluding sentence** — summarises or transitions"),

            FixedLesson(EngL4Id, EnglishG5Ch2Id, "Using Descriptive Language", 2, 25,
                "## Descriptive Language\n\nDescriptive writing uses **adjectives**, **adverbs** " +
                "and **figurative language** (similes, metaphors) to paint a vivid picture for the reader."));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 8 lessons.");
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    private async Task SeedQuestionsAsync(CancellationToken ct)
    {
        if (await _db.Questions.AnyAsync(ct)) return;

        _db.Questions.AddRange(

            // Math L1 — Understanding Place Value
            FixedQuestion(MathL1Id, "What is the place value of the digit 3 in the number 4,325?",
                QuestionType.MultipleChoice, DifficultyLevel.Easy,
                "[\"Ones\",\"Tens\",\"Hundreds\",\"Thousands\"]", "2"),

            FixedQuestion(MathL1Id, "In the number 7,098, the digit 0 is in the tens place.",
                QuestionType.TrueFalse, DifficultyLevel.Easy,
                null, "false"),

            // Math L2 — Comparing and Ordering Numbers
            FixedQuestion(MathL2Id, "Which symbol correctly completes: 3,412 __ 3,421?",
                QuestionType.MultipleChoice, DifficultyLevel.Easy,
                "[\">\" ,\"<\",\"=\",\"≥\"]", "1"),

            FixedQuestion(MathL2Id, "5,000 is greater than 4,999.",
                QuestionType.TrueFalse, DifficultyLevel.Easy,
                null, "true"),

            // Math L3 — Column Addition
            FixedQuestion(MathL3Id, "What is 2,456 + 1,378?",
                QuestionType.MultipleChoice, DifficultyLevel.Medium,
                "[\"3,724\",\"3,834\",\"3,844\",\"3,934\"]", "1"),

            FixedQuestion(MathL3Id, "Explain in your own words why we carry over when adding columns.",
                QuestionType.ShortAnswer, DifficultyLevel.Medium,
                null, "We carry over because when a column total reaches 10 or more, the extra tens move to the next column left."),

            // Math L4 — Column Subtraction
            FixedQuestion(MathL4Id, "What is 5,002 − 1,348?",
                QuestionType.MultipleChoice, DifficultyLevel.Medium,
                "[\"3,654\",\"3,754\",\"3,664\",\"3,644\"]", "0"),

            FixedQuestion(MathL4Id, "Borrowing in subtraction always reduces the digit to the left by 1.",
                QuestionType.TrueFalse, DifficultyLevel.Easy,
                null, "true"),

            // English L1 — Finding the Main Idea
            FixedQuestion(EngL1Id, "Where is the main idea of a paragraph most commonly found?",
                QuestionType.MultipleChoice, DifficultyLevel.Easy,
                "[\"In the middle\",\"In the first or last sentence\",\"In every sentence\",\"Between paragraphs\"]", "1"),

            FixedQuestion(EngL1Id, "The main idea is always explicitly stated and never implied.",
                QuestionType.TrueFalse, DifficultyLevel.Medium,
                null, "false"),

            // English L2 — Making Inferences
            FixedQuestion(EngL2Id, "An inference is based on text clues and the reader's prior knowledge.",
                QuestionType.TrueFalse, DifficultyLevel.Easy,
                null, "true"),

            FixedQuestion(EngL2Id, "Write an inference you could make if a character in a story is wearing a heavy coat and carrying an umbrella.",
                QuestionType.ShortAnswer, DifficultyLevel.Medium,
                null, "It is likely cold and rainy outside."),

            // English L3 — Paragraph Structure
            FixedQuestion(EngL3Id, "Which part of a paragraph states the main idea?",
                QuestionType.MultipleChoice, DifficultyLevel.Easy,
                "[\"Concluding sentence\",\"Supporting sentences\",\"Topic sentence\",\"Transition word\"]", "2"),

            FixedQuestion(EngL3Id, "A paragraph can have more than one topic sentence.",
                QuestionType.TrueFalse, DifficultyLevel.Easy,
                null, "false"),

            // English L4 — Descriptive Language
            FixedQuestion(EngL4Id, "Which of the following is an example of a simile?",
                QuestionType.MultipleChoice, DifficultyLevel.Medium,
                "[\"The sun set.\",\"Her smile was sunshine.\",\"She ran as fast as the wind.\",\"It was a dark day.\"]", "2"),

            FixedQuestion(EngL4Id, "Adjectives describe verbs.",
                QuestionType.TrueFalse, DifficultyLevel.Easy,
                null, "false"));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 16 questions.");
    }

    // ── Profiles ──────────────────────────────────────────────────────────────

    private async Task SeedProfilesAsync(CancellationToken ct)
    {
        if (await _db.ParentProfiles.AnyAsync(ct)) return;

        // Parent profiles
        _db.ParentProfiles.Add(FixedParentProfile(ParentProfileAId, ParentAId));
        _db.ParentProfiles.Add(FixedParentProfile(ParentProfileBId, ParentBId));

        // Student profiles — linked to parents and Grade 5
        _db.StudentProfiles.Add(FixedStudentProfile(StudentProfileAId, StudentAId, Grade5Id, ParentProfileAId));
        _db.StudentProfiles.Add(FixedStudentProfile(StudentProfileBId, StudentBId, Grade5Id, ParentProfileBId));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 2 parent profiles and 2 student profiles.");
    }

    // ── Enrollments ───────────────────────────────────────────────────────────

    private async Task SeedEnrollmentsAsync(CancellationToken ct)
    {
        if (await _db.Enrollments.AnyAsync(ct)) return;

        // Student A enrolled in both subjects
        var enrollMath    = Enrollment.Create(StudentProfileAId, MathId);
        var enrollEnglish = Enrollment.Create(StudentProfileAId, EnglishId);

        // Student B enrolled in Mathematics only
        var enrollMathB = Enrollment.Create(StudentProfileBId, MathId);

        _db.Enrollments.AddRange(enrollMath, enrollEnglish, enrollMathB);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 3 enrollments.");
    }

    // ── Progress ──────────────────────────────────────────────────────────────

    private async Task SeedProgressAsync(CancellationToken ct)
    {
        if (await _db.LessonProgress.AnyAsync(ct)) return;

        // Student A: Lesson 1 started (InProgress), Lesson 2 completed with a score
        var p1 = LessonProgress.Create(StudentProfileAId, MathL1Id);
        p1.Start();

        var p2 = LessonProgress.Create(StudentProfileAId, MathL2Id);
        p2.Complete(85m);

        _db.LessonProgress.AddRange(p1, p2);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 2 lesson progress records.");
    }

    // ── Private factory helpers ───────────────────────────────────────────────
    // These helpers bypass the domain static Create() factory so we can inject
    // fixed GUIDs.  This is seed-data-only; application code always uses Create().

    private static Grade FixedGrade(Guid id, string name, int level)
    {
        var g = Grade.Create(name, level);
        SetId(g, id);
        return g;
    }

    private static Subject FixedSubject(Guid id, string name, string slug, string? description)
    {
        var s = Subject.Create(name, slug, description);
        SetId(s, id);
        return s;
    }

    private User FixedUser(Guid id, string email, string rawPassword, UserRole role,
                           string firstName, string lastName)
    {
        var u = User.Create(email, _hasher.Hash(rawPassword), role,
                            firstName: firstName, lastName: lastName);
        SetId(u, id);
        return u;
    }

    private static Chapter FixedChapter(Guid id, Guid subjectId, Guid gradeId, string title, int order)
    {
        var c = Chapter.Create(subjectId, gradeId, title, order);
        SetId(c, id);
        return c;
    }

    private static Lesson FixedLesson(Guid id, Guid chapterId, string title, int order,
                                      int estimatedMinutes, string content)
    {
        var l = Lesson.Create(chapterId, title, order, content, estimatedMinutes);
        SetId(l, id);
        return l;
    }

    private static Question FixedQuestion(Guid lessonId, string text, QuestionType type,
                                          DifficultyLevel difficulty,
                                          string? optionsJson, string correctAnswer)
    {
        // Questions are always linked to a lesson in seed data.
        return Question.Create(text, type, difficulty, correctAnswer, lessonId, optionsJson);
    }

    private static ParentProfile FixedParentProfile(Guid id, Guid userId)
    {
        var p = ParentProfile.Create(userId);
        SetId(p, id);
        return p;
    }

    private static StudentProfile FixedStudentProfile(Guid id, Guid userId, Guid gradeId, Guid parentId)
    {
        var s = StudentProfile.Create(userId, gradeId, parentId);
        SetId(s, id);
        return s;
    }

    /// <summary>
    /// Sets the Id of a domain entity by writing directly to the compiler-generated
    /// backing field ("<Id>k__BackingField").  This is necessary because the Entity
    /// base class declares the setter as protected — PropertyInfo.SetValue would throw
    /// MethodAccessException when invoked from outside the class hierarchy.
    /// Only ever used by seed data — application code uses the Create() factory.
    /// </summary>
    private static void SetId<T>(T entity, Guid id) where T : Domain.Common.Entity
    {
        var field = typeof(Domain.Common.Entity)
            .GetField("<Id>k__BackingField",
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                "Could not find backing field '<Id>k__BackingField' on Entity. " +
                "The compiler may have changed the field name.");

        field.SetValue(entity, id);
    }
}
