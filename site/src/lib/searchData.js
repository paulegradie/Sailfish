// Search data for documentation
// This would ideally be generated at build time from the actual documentation files

export const searchData = [
  // Introduction Section
  {
    id: 'when-to-use-sailfish',
    title: 'When To Use Sailfish',
    url: '/docs/0/when-to-use-sailfish',
    section: 'Introduction',
    content: 'Sailfish is ideal for performance testing .NET applications when you need statistical analysis, regression detection, and machine learning insights.',
    keywords: ['performance testing', 'when to use', 'benefits', 'use cases']
  },
  {
    id: 'getting-started',
    title: 'Getting Started',
    url: '/docs/0/getting-started',
    section: 'Introduction',
    content: 'Quick start guide to begin using Sailfish for performance testing. Install the NuGet package and write your first performance test.',
    keywords: ['getting started', 'quick start', 'installation', 'first test']
  },
  {
    id: 'installation',
    title: 'Installation',
    url: '/docs/0/installation',
    section: 'Introduction',
    content: 'Install Sailfish via NuGet package manager. Supports .NET Core and .NET Framework applications.',
    keywords: ['installation', 'nuget', 'setup', 'requirements']
  },
  {
    id: 'quick-start',
    title: 'Quick Start',
    url: '/docs/0/quick-start',
    section: 'Introduction',
    content: 'Write your first Sailfish performance test in minutes. Simple attribute-based configuration.',
    keywords: ['quick start', 'first test', 'example', 'tutorial']
  },
  {
    id: 'essential-information',
    title: 'Essential Information',
    url: '/docs/0/essential-information',
    section: 'Introduction',
    content: 'Important concepts and best practices for effective performance testing with Sailfish.',
    keywords: ['best practices', 'concepts', 'important', 'essential']
  },
  {
    id: 'license',
    title: 'License',
    url: '/docs/0/license',
    section: 'Introduction',
    content: 'Sailfish licensing information. Open source MIT license for individuals and small teams, enterprise license for larger organizations.',
    keywords: ['license', 'MIT', 'enterprise', 'pricing']
  },

  // Sailfish Basics Section
  {
    id: 'required-attributes',
    title: 'Required Attributes',
    url: '/docs/1/required-attributes',
    section: 'Sailfish Basics',
    content: 'Learn about the required attributes for Sailfish performance tests: [Sailfish] and [SailfishMethod].',
    keywords: ['attributes', 'Sailfish', 'SailfishMethod', 'required', 'configuration']
  },
  {
    id: 'sailfish-variables',
    title: 'Sailfish Variables',
    url: '/docs/1/sailfish-variables',
    section: 'Sailfish Basics',
    content: 'Use [SailfishVariable] to parameterize your performance tests and test different scenarios.',
    keywords: ['variables', 'SailfishVariable', 'parameters', 'scenarios']
  },
  {
    id: 'sailfish-test-lifecycle',
    title: 'Sailfish Test Lifecycle',
    url: '/docs/1/sailfish-test-lifecycle',
    section: 'Sailfish Basics',
    content: 'Understand the test lifecycle with setup and teardown methods: GlobalSetup, MethodSetup, MethodTeardown.',
    keywords: ['lifecycle', 'setup', 'teardown', 'GlobalSetup', 'MethodSetup']
  },
  {
    id: 'test-dependencies',
    title: 'Test Dependencies',
    url: '/docs/1/test-dependencies',
    section: 'Sailfish Basics',
    content: 'Manage dependencies and external resources in your performance tests.',
    keywords: ['dependencies', 'resources', 'injection', 'external']
  },
  {
    id: 'output-attributes',
    title: 'Output Attributes',
    url: '/docs/1/output-attributes',
    section: 'Sailfish Basics',
    content: 'Configure output formats and customize how Sailfish reports performance results.',
    keywords: ['output', 'results', 'reporting', 'formats', 'customization']
  },

  // Features Section
  {
    id: 'sailfish-core',
    title: 'Sailfish Core Features',
    url: '/docs/2/sailfish',
    section: 'Features',
    content: 'Core Sailfish functionality including statistical analysis, outlier detection, and performance measurement.',
    keywords: ['core features', 'statistical analysis', 'outlier detection', 'measurement']
  },
  {
    id: 'saildiff',
    title: 'SailDiff - Regression Detection',
    url: '/docs/2/saildiff',
    section: 'Features',
    content: 'SailDiff automatically detects performance regressions by comparing current results with previous runs.',
    keywords: ['SailDiff', 'regression detection', 'comparison', 'before after']
  },
  {
    id: 'scalefish',
    title: 'ScaleFish - ML Analysis',
    url: '/docs/2/scalefish',
    section: 'Features',
    content: 'ScaleFish uses machine learning to predict performance at different scales and estimate algorithmic complexity.',
    keywords: ['ScaleFish', 'machine learning', 'scaling', 'complexity', 'prediction']
  },

  // Advanced Section
  {
    id: 'extensibility',
    title: 'Extensibility',
    url: '/docs/3/extensibility',
    section: 'Advanced',
    content: 'Extend Sailfish with custom analyzers, output formats, and integrations.',
    keywords: ['extensibility', 'custom', 'analyzers', 'integrations', 'plugins']
  },
  {
    id: 'example-app',
    title: 'Example Application',
    url: '/docs/3/example-app',
    section: 'Advanced',
    content: 'Complete example application demonstrating Sailfish performance testing best practices.',
    keywords: ['example', 'sample', 'application', 'demo', 'best practices']
  },

  // Project Section
  {
    id: 'release-notes',
    title: 'Release Notes',
    url: '/docs/4/releasenotes',
    section: 'Project',
    content: 'Latest updates, new features, and changes in Sailfish releases.',
    keywords: ['release notes', 'updates', 'changelog', 'new features', 'versions']
  },

  // Marketing Pages
  {
    id: 'pricing',
    title: 'Pricing',
    url: '/pricing',
    section: 'Marketing',
    content: 'Sailfish pricing plans: Free open source for individuals and small teams, Enterprise for larger organizations.',
    keywords: ['pricing', 'plans', 'enterprise', 'free', 'cost']
  },
  {
    id: 'enterprise',
    title: 'Enterprise Solutions',
    url: '/enterprise',
    section: 'Marketing',
    content: 'Enterprise features including advanced analytics, team management, priority support, and compliance.',
    keywords: ['enterprise', 'business', 'team management', 'support', 'compliance']
  },
  {
    id: 'case-studies',
    title: 'Customer Success Stories',
    url: '/case-studies',
    section: 'Marketing',
    content: 'Real-world examples of companies using Sailfish to improve application performance and development velocity.',
    keywords: ['case studies', 'success stories', 'customers', 'examples', 'results']
  },
  {
    id: 'comparison',
    title: 'Sailfish vs Alternatives',
    url: '/comparison',
    section: 'Marketing',
    content: 'Compare Sailfish with BenchmarkDotNet, NBomber, k6, and other performance testing tools.',
    keywords: ['comparison', 'vs', 'alternatives', 'BenchmarkDotNet', 'NBomber', 'k6']
  }
]

// Search configuration for Fuse.js
export const searchOptions = {
  keys: [
    {
      name: 'title',
      weight: 0.4
    },
    {
      name: 'content',
      weight: 0.3
    },
    {
      name: 'keywords',
      weight: 0.2
    },
    {
      name: 'section',
      weight: 0.1
    }
  ],
  threshold: 0.3,
  includeScore: true,
  includeMatches: true,
  minMatchCharLength: 2,
  shouldSort: true,
  findAllMatches: true
}

// Popular searches for quick access
export const popularSearches = [
  'getting started',
  'installation',
  'SailfishVariable',
  'regression detection',
  'machine learning',
  'enterprise features',
  'pricing',
  'comparison'
]

// Quick links for empty search state
export const quickLinks = [
  {
    title: 'Getting Started',
    url: '/docs/0/getting-started',
    description: 'Quick start guide for new users'
  },
  {
    title: 'Installation',
    url: '/docs/0/installation',
    description: 'How to install Sailfish'
  },
  {
    title: 'API Reference',
    url: '/docs/2/sailfish',
    description: 'Complete API documentation'
  },
  {
    title: 'Examples',
    url: '/docs/3/example-app',
    description: 'Sample applications and code'
  },
  {
    title: 'Enterprise Features',
    url: '/enterprise',
    description: 'Advanced capabilities for teams'
  },
  {
    title: 'Pricing',
    url: '/pricing',
    description: 'Plans and pricing information'
  }
]
