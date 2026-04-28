import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { StatCard, Badge, SectionCard, PlaceholderRow } from '@/components/ui'

/**
 * Admin Dashboard — placeholder composition.
 *
 * Sections mirror the product areas Phase 2+ will make functional:
 *   - Users & Roles    (user accounts across all tenants)
 *   - Tenant Management (school / organisation accounts)
 *   - Courses & Curriculum (published and draft content)
 *   - Content Library   (media, resources, materials)
 *   - System Status     (service health overview)
 *
 * Phase 2: replace PlaceholderRow lists with real API-driven components.
 * The SectionCard structure, stat grid, and PageContainer schema stay unchanged.
 */
export default function AdminDashboardPage() {
  const { t } = useTranslation()

  return (
    <PageContainer
      title={t('dashboards.admin.title')}
      description={t('dashboards.admin.description')}
    >
      <div className="space-y-6">
        {/* ── Stat row ──────────────────────────────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label={t('dashboards.admin.stats.tenants')}    value="—" />
          <StatCard label={t('dashboards.admin.stats.users')}      value="—" />
          <StatCard label={t('dashboards.admin.stats.courses')}    value="—" />
          <StatCard label={t('dashboards.admin.stats.aiSessions')} value="—" />
        </div>

        {/* ── Users & Tenants row ───────────────────────────────────────── */}
        <div className="grid gap-4 md:grid-cols-2">
          <SectionCard
            title={t('dashboards.admin.sections.usersRoles')}
            description="Active accounts across all tenants"
          >
            <PlaceholderRow label="Tenant Admins"   meta="Manage schools"      accent="brand"   tag="role" />
            <PlaceholderRow label="Content Editors" meta="Manage curriculum"   accent="info"    tag="role" />
            <PlaceholderRow label="Teachers"        meta="Active instructors"  accent="success" tag="role" />
            <PlaceholderRow label="Students"        meta="Current learners"    accent="warning" tag="role" />
          </SectionCard>

          <SectionCard
            title={t('dashboards.admin.sections.tenantManagement')}
            description="Schools and organisations on the platform"
          >
            <PlaceholderRow label="School of Excellence"  meta="Active tenant"  accent="success" tag="tenant" />
            <PlaceholderRow label="Greenfield Academy"    meta="Active tenant"  accent="success" tag="tenant" />
            <PlaceholderRow label="Sunrise International" meta="Pending setup"  accent="warning" tag="tenant" />
          </SectionCard>
        </div>

        {/* ── Content row ───────────────────────────────────────────────── */}
        <div className="grid gap-4 md:grid-cols-2">
          <SectionCard
            title={t('dashboards.admin.sections.coursesCurriculum')}
            description="Published and draft course content"
          >
            <PlaceholderRow label="Mathematics — Grade 7" meta="21 lessons · Published" accent="brand" tag="course" />
            <PlaceholderRow label="English Language Arts"  meta="18 lessons · Published" accent="brand" tag="course" />
            <PlaceholderRow label="Science & Technology"   meta="Draft"                  accent="muted" tag="course" />
          </SectionCard>

          <SectionCard
            title={t('dashboards.admin.sections.contentLibrary')}
            description="Media, resources, and materials"
          >
            <PlaceholderRow label="Video Lessons"        meta="Hosted media"            accent="info"  tag="media"    />
            <PlaceholderRow label="PDF Materials"        meta="Downloadable resources"  accent="info"  tag="resource" />
            <PlaceholderRow label="AI-Generated Quizzes" meta="Assessments"             accent="brand" tag="ai"       />
          </SectionCard>
        </div>

        {/* ── System Status — full width ────────────────────────────────── */}
        <SectionCard
          title={t('dashboards.admin.sections.systemStatus')}
          description="Service health overview"
        >
          <div className="flex flex-wrap gap-2">
            <Badge variant="success">Backend API ✓</Badge>
            <Badge variant="success">AI Service ✓</Badge>
            <Badge variant="info">Phase 1 — Complete</Badge>
            <Badge variant="warning">Auth — Phase 2</Badge>
          </div>
          <p className="mt-3 text-sm text-content-secondary">
            Production services will display live health indicators in Phase 2.
          </p>
        </SectionCard>
      </div>
    </PageContainer>
  )
}

