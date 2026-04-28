import type { ReactNode } from 'react'
import { Card, CardHeader, CardBody } from './Card'

// ── Props ─────────────────────────────────────────────────────────────────────

interface SectionCardProps {
  /** Section heading — rendered as <h3> inside CardHeader. */
  title:         string
  /** Optional supporting text below the title. */
  description?:  string
  /** Optional element rendered at the trailing edge of the header (CTA, link…). */
  action?:       ReactNode
  children:      ReactNode
  /** Applied to the outer <Card>. */
  className?:    string
  /** Applied to the inner <CardBody>. */
  bodyClassName?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * SectionCard — the primary container for a named content section within a page.
 *
 * A convenience composition of Card + CardHeader + CardBody. Use this instead
 * of assembling the three primitives manually when all you need is a titled
 * block with optional action:
 *
 *   <SectionCard title="Recent Activity" action={<Button size="sm">View all</Button>}>
 *     <ActivityList />
 *   </SectionCard>
 *
 * When you need multiple body zones (e.g. a card with a chart and a separate
 * data table, each with their own padding), compose Card/CardHeader/CardBody
 * directly instead.
 */
export function SectionCard({
  title,
  description,
  action,
  children,
  className,
  bodyClassName,
}: SectionCardProps) {
  return (
    <Card className={className}>
      <CardHeader title={title} description={description} action={action} />
      <CardBody className={bodyClassName}>{children}</CardBody>
    </Card>
  )
}
