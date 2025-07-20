import { Callout } from '@/components/Callout'
import { QuickLink, QuickLinks } from '@/components/QuickLinks'
import {
  InfoCallout,
  WarningCallout,
  SuccessCallout,
  ErrorCallout,
  TipCallout,
  NoteCallout,
  CodeCallout,
  ExampleCallout,
  Steps,
  Step,
  FeatureGrid,
  FeatureCard
} from '@/components/ui/Callout'

const tags = {
  callout: {
    attributes: {
      title: { type: String },
      type: {
        type: String,
        default: 'note',
        matches: ['note', 'warning'],
        errorLevel: 'critical',
      },
    },
    render: Callout,
  },
  figure: {
    selfClosing: true,
    attributes: {
      src: { type: String },
      alt: { type: String },
      caption: { type: String },
    },
    render: ({ src, alt = '', caption }) => (
      <figure>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={src} alt={alt} />
        <figcaption>{caption}</figcaption>
      </figure>
    ),
  },
  'quick-links': {
    render: QuickLinks,
  },
  'quick-link': {
    selfClosing: true,
    render: QuickLink,
    attributes: {
      title: { type: String },
      description: { type: String },
      icon: { type: String },
      href: { type: String },
    },
  },
  // Enhanced callout components
  'info-callout': {
    render: InfoCallout,
    attributes: {
      title: { type: String },
    },
  },
  'warning-callout': {
    render: WarningCallout,
    attributes: {
      title: { type: String },
    },
  },
  'success-callout': {
    render: SuccessCallout,
    attributes: {
      title: { type: String },
    },
  },
  'error-callout': {
    render: ErrorCallout,
    attributes: {
      title: { type: String },
    },
  },
  'tip-callout': {
    render: TipCallout,
    attributes: {
      title: { type: String },
    },
  },
  'note-callout': {
    render: NoteCallout,
    attributes: {
      title: { type: String },
    },
  },
  'code-callout': {
    render: CodeCallout,
    attributes: {
      title: { type: String },
    },
  },
  'example-callout': {
    render: ExampleCallout,
    attributes: {
      title: { type: String },
    },
  },
  // Steps component
  'steps': {
    render: Steps,
  },
  'step': {
    render: Step,
    attributes: {
      title: { type: String },
    },
  },
  // Feature components
  'feature-grid': {
    render: FeatureGrid,
    attributes: {
      columns: { type: Number, default: 2 },
    },
  },
  'feature-card': {
    render: FeatureCard,
    attributes: {
      title: { type: String },
      description: { type: String },
    },
  },
}

export default tags
