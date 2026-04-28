import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { StatCard, SectionCard, PlaceholderRow } from '@/components/ui'

/**
 * Parent Dashboard — placeholder composition.
 *
 * Sections mirror the product areas Phase 2+ will make functional:
 *   - Children Overview  (per-child progress summary)
 *   - Weekly Schedule    (upcoming lessons for all children)
 *   - Recent Lessons     (latest activity across children)
 *   - Reminders & Alerts (notices, due dates, meetings)
 *
 * Phase 2: replace PlaceholderRow lists with real API-driven components.
 */
export default function ParentDashboardPage() {
  const { t } = useTranslation()

  return (
    <PageContainer
      title={t('dashboards.parent.title')}
      description={t('dashboards.parent.description')}
    >
      <div className="space-y-6">
        {/* ── Stat row ──────────────────────────────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label={t('dashboards.parent.stats.children')}         value="—" />
          <StatCard label={t('dashboards.parent.stats.lessonsCompleted')} value="—" />
          <StatCard label={t('dashboards.parent.stats.weeklyTime')}       value="—" />
          <StatCard label={t('dashboards.parent.stats.overallProgress')}  value="—" />
        </div>

        {/* ── Children Overview + Schedule row ─────────────────────────── */}
        <div className="grid gap-4 md:grid-cols-2">
          <SectionCard
            title={t('dashboards.parent.sections.childrenOverview')}
            description="Progress summary per child"
          >
            <PlaceholderRow label="Ahmed" meta="Grade 7 · Mathematics focus" accent="brand"   tag="child" />
            <PlaceholderRow label="Layla" meta="Grade 5 · English focus"     accent="info"    tag="child" />
            <PlaceholderRow label="Omar"  meta="Grade 3 · Science focus"     accent="success" tag="child" />
          </SectionCard>

          <SectionCard
            title={t('dashboards.parent.sections.weeklySchedule')}
            description="Upcoming lessons this week"
          >
            <PlaceholderRow label="Mathematics — Fractions" meta="Monday · Ahmed"    accent="brand"   />
            <PlaceholderRow label="English Reading"          meta="Wednesday · Layla" accent="info"    />
            <PlaceholderRow label="Science — Ecosystems"     meta="Friday · Ahmed"    accent="success" />
          </SectionCard>
        </div>

        {/* ── Recent Lessons + Reminders row ───────────────────────────── */}
        <div className="grid gap-4 md:grid-cols-2">
          <SectionCard
            title={t('dashboards.parent.sections.recentLessons')}
            description="Your children's latest activity"
          >
            <PlaceholderRow label="Ch. 5 — Fractions"              meta="Ahmed · Completed"    accent="success" />
            <PlaceholderRow label="Reading Comprehension Task #3"   meta="Layla · In progress"  accent="warning" />
            <PlaceholderRow label="Lab Report — Ecosystems"         meta="Ahmed · Submitted"    accent="brand"   />
          </SectionCard>

          <SectionCard
            title={t('dashboards.parent.sections.reminders')}
            description="Upcoming actions and notices"
          >
            <PlaceholderRow label="Term Report Available"  meta="Download this week"         accent="info"    />
            <PlaceholderRow label="Parent-Teacher Meeting" meta="Next Thursday"               accent="warning" />
            <PlaceholderRow label="Assignment Due — Ahmed" meta="Chapter 5 Quiz · Tomorrow"  accent="warning" />
          </SectionCard>
        </div>
      </div>
    </PageContainer>
  )
}
