import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { StatCard, SectionCard, PlaceholderRow } from '@/components/ui'

/**
 * Teacher Dashboard — placeholder composition.
 *
 * Sections mirror the product areas Phase 2+ will make functional:
 *   - My Classes        (enrolled students by class group)
 *   - Upcoming Lessons  (this week's scheduled sessions)
 *   - Flagged Learners  (students needing attention)
 *   - Open Assignments  (awaiting student submissions)
 *   - Recent Submissions (latest student work)
 *
 * Phase 2: replace PlaceholderRow lists with real API-driven components.
 */
export default function TeacherDashboardPage() {
  const { t } = useTranslation()

  return (
    <PageContainer
      title={t('dashboards.teacher.title')}
      description={t('dashboards.teacher.description')}
    >
      <div className="space-y-6">
        {/* ── Stat row ──────────────────────────────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label={t('dashboards.teacher.stats.students')}    value="—" />
          <StatCard label={t('dashboards.teacher.stats.lessons')}     value="—" />
          <StatCard label={t('dashboards.teacher.stats.assignments')} value="—" />
          <StatCard label={t('dashboards.teacher.stats.avgScore')}    value="—" />
        </div>

        {/* ── Classes / Lessons / Flagged — 3 cols on lg ──────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <SectionCard
            title={t('dashboards.teacher.sections.myClasses')}
            description="Enrolled students by class"
          >
            <PlaceholderRow label="Grade 7A — Mathematics" meta="24 students" accent="brand" />
            <PlaceholderRow label="Grade 8 — English"      meta="18 students" accent="brand" />
            <PlaceholderRow label="Advanced Science"       meta="12 students" accent="info"  />
          </SectionCard>

          <SectionCard
            title={t('dashboards.teacher.sections.upcomingLessons')}
            description="Scheduled for this week"
          >
            <PlaceholderRow label="Mathematics Ch. 5 — Fractions" meta="Tomorrow" accent="warning" />
            <PlaceholderRow label="English Reading Comprehension"  meta="Thursday" accent="brand"   />
            <PlaceholderRow label="Science Lab — Ecosystems"       meta="Friday"   accent="info"    />
          </SectionCard>

          <SectionCard
            title={t('dashboards.teacher.sections.flaggedLearners')}
            description="Students needing attention"
          >
            <PlaceholderRow label="Ali Hassan"     meta="Below average · 2 weeks"  accent="warning" />
            <PlaceholderRow label="Sara Mohammadi" meta="Missed 3 assignments"     accent="error"   />
            <PlaceholderRow label="James Okonkwo"  meta="Great progress this week" accent="success" />
          </SectionCard>
        </div>

        {/* ── Assignments + Submissions row ────────────────────────────── */}
        <div className="grid gap-4 md:grid-cols-2">
          <SectionCard
            title={t('dashboards.teacher.sections.openAssignments')}
            description="Awaiting student submissions"
          >
            <PlaceholderRow label="Chapter 5 Quiz"      meta="Due tomorrow · 12 pending"  accent="warning" tag="maths"   />
            <PlaceholderRow label="Reading Task #3"      meta="Due Friday · 8 pending"     accent="brand"   tag="english" />
            <PlaceholderRow label="Ecosystems Worksheet" meta="Due next week · 0 pending"  accent="info"    tag="science" />
          </SectionCard>

          <SectionCard
            title={t('dashboards.teacher.sections.recentSubmissions')}
            description="Latest student work"
          >
            <PlaceholderRow label="Ahmed Al-Rashidi" meta="Chapter 4 Quiz · 2 h ago"  accent="success" />
            <PlaceholderRow label="Fatima Nouri"     meta="Reading Task #2 · 4 h ago" accent="success" />
            <PlaceholderRow label="David Osei"       meta="Lab Report #1 · Yesterday" accent="brand"   />
          </SectionCard>
        </div>
      </div>
    </PageContainer>
  )
}
