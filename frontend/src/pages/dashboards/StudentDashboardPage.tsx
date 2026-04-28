import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { StatCard, SectionCard, PlaceholderRow, Badge } from '@/components/ui'

/**
 * Student Dashboard — placeholder composition.
 *
 * Sections mirror the product areas Phase 2+ will make functional:
 *   - Continue Learning  (lesson queue, resume in-progress lessons)
 *   - AI Tutor          (conversational AI assistant)
 *   - My Subjects       (enrolled courses + current grade)
 *   - My Progress       (completion tracking, streaks, skill milestones)
 *   - My Goals          (weekly targets and achievements)
 *
 * Phase 2: replace PlaceholderRow lists with real API-driven components.
 * Continue Learning becomes a lesson card queue.
 * AI Tutor becomes the chat entry point.
 */
export default function StudentDashboardPage() {
  const { t } = useTranslation()

  return (
    <PageContainer
      title={t('dashboards.student.title')}
      description={t('dashboards.student.description')}
    >
      <div className="space-y-6">
        {/* ── Stat row ──────────────────────────────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label={t('dashboards.student.stats.streak')}           value="—" />
          <StatCard label={t('dashboards.student.stats.lessonsCompleted')} value="—" />
          <StatCard label={t('dashboards.student.stats.points')}           value="—" />
          <StatCard label={t('dashboards.student.stats.level')}            value="—" />
        </div>

        {/* ── Continue Learning (2/3) + AI Tutor (1/3) ─────────────────── */}
        <div className="grid gap-4 lg:grid-cols-3">
          <div className="lg:col-span-2">
            <SectionCard
              title={t('dashboards.student.sections.continueLearning')}
              description="Pick up where you left off"
              className="h-full"
            >
              <PlaceholderRow
                label="Mathematics — Ch. 5: Fractions"
                meta="Lesson 4 of 8 · In progress"
                accent="brand"
              />
              <PlaceholderRow
                label="English — Reading Comprehension Unit 3"
                meta="New assignment available"
                accent="warning"
              />
              <PlaceholderRow
                label="Science — Ecosystems & the Environment"
                meta="Completed"
                accent="success"
              />
            </SectionCard>
          </div>

          <SectionCard
            title={t('dashboards.student.sections.aiTutor')}
            description="Ask your AI assistant a question"
            className="h-full"
          >
            <p className="text-sm text-content-secondary">
              The AI tutoring assistant will be available in Phase 2. You will
              be able to ask questions about your lessons and get instant feedback.
            </p>
            <Badge variant="info" className="mt-3">Phase 2</Badge>
          </SectionCard>
        </div>

        {/* ── Subjects / Progress / Goals row ──────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <SectionCard
            title={t('dashboards.student.sections.mySubjects')}
            description="Enrolled courses"
          >
            <PlaceholderRow label="Mathematics" meta="Grade A"  accent="brand"   />
            <PlaceholderRow label="English"     meta="Grade B+" accent="info"    />
            <PlaceholderRow label="Science"     meta="Grade A−" accent="success" />
          </SectionCard>

          <SectionCard
            title={t('dashboards.student.sections.myProgress')}
            description="Overall learning progress"
          >
            <PlaceholderRow label="Overall Completion" meta="Progress charts — Phase 2" accent="brand"   />
            <PlaceholderRow label="Streak Tracker"     meta="Daily goal — Phase 2"      accent="warning" />
            <PlaceholderRow label="Skill Milestones"   meta="Phase 2"                   accent="info"    />
          </SectionCard>

          <SectionCard
            title={t('dashboards.student.sections.myGoals')}
            description="Weekly targets"
          >
            <PlaceholderRow label="Complete 5 lessons this week" meta="2 / 5"    accent="brand"   />
            <PlaceholderRow label="Earn 100 points"               meta="67 / 100" accent="warning" />
            <PlaceholderRow label="Finish Mathematics chapter"    meta="Pending"  accent="info"    />
          </SectionCard>
        </div>
      </div>
    </PageContainer>
  )
}
