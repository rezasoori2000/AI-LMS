import { useNavigate }   from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer }  from '@/components/layout/PageContainer'
import { StateWrapper }   from '@/components/feedback'
import { StatCard, SectionCard, PlaceholderRow, Button } from '@/components/ui'
import { useMyChildren }  from '@/features/parent/hooks/useParent'

/**
 * Parent Dashboard — live Children Overview + stat row.
 *
 * Connected sections (Phase 1, Section 6):
 *   - Stat row: children count and total lessons completed from the API
 *   - Children Overview: real children list with quick navigate to detail
 *
 * Placeholder sections (Phase 2):
 *   - Weekly Schedule    (requires scheduling/timetable feature)
 *   - Recent Lessons     (requires activity feed feature)
 *   - Reminders & Alerts (requires notification feature)
 */
export default function ParentDashboardPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()

  const { data: children, isLoading, isError, refetch } = useMyChildren()

  const childrenCount          = children?.length ?? 0
  const totalLessonsCompleted  = children?.reduce((sum, c) => sum + c.lessonsCompleted, 0) ?? 0

  return (
    <PageContainer
      title={t('dashboards.parent.title')}
      description={t('dashboards.parent.description')}
    >
      <div className="space-y-6">
        {/* ── Stat row ──────────────────────────────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            label={t('dashboards.parent.stats.children')}
            value={isLoading ? '…' : String(childrenCount)}
          />
          <StatCard
            label={t('dashboards.parent.stats.lessonsCompleted')}
            value={isLoading ? '…' : String(totalLessonsCompleted)}
          />
          <StatCard label={t('dashboards.parent.stats.weeklyTime')}      value="—" />
          <StatCard label={t('dashboards.parent.stats.overallProgress')} value="—" />
        </div>

        {/* ── Children Overview + Schedule row ─────────────────────────── */}
        <div className="grid gap-4 md:grid-cols-2">
          <SectionCard
            title={t('dashboards.parent.sections.childrenOverview')}
            description={t('dashboards.parent.sections.childrenOverviewDesc')}
            action={
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate('/parent/children')}
              >
                {t('parent.children.viewAll')}
              </Button>
            }
          >
            <StateWrapper
              isLoading={isLoading}
              isError={isError}
              isEmpty={!children?.length}
              emptyTitle={t('parent.children.empty')}
              emptyDescription={t('parent.children.emptyDescription')}
              onRetry={refetch}
            >
              <ul className="-mx-5 -mb-4 divide-y divide-stroke" role="list">
                {children?.map(child => (
                  <li
                    key={child.studentId}
                    className="flex cursor-pointer items-center gap-3 px-5 py-3 hover:bg-surface-raised transition-colors"
                    onClick={() => navigate(`/parent/children/${child.studentId}`)}
                  >
                    <span
                      aria-hidden="true"
                      className="flex h-7 w-7 shrink-0 select-none items-center justify-center rounded-full bg-brand-100 text-xs font-bold uppercase text-brand-700"
                    >
                      {child.fullName.charAt(0)}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium text-content-primary">
                        {child.fullName}
                      </p>
                      <p className="text-xs text-content-secondary">
                        {child.gradeName ?? '—'}
                        {' · '}
                        {child.activeEnrollments} {t('parent.children.enrollments')}
                        {' · '}
                        {child.lessonsCompleted} {t('parent.children.lessonsCompleted')}
                      </p>
                    </div>
                  </li>
                ))}
              </ul>
            </StateWrapper>
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

