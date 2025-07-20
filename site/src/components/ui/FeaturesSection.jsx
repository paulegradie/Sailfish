import { useState } from 'react'
import {
  ChartBarIcon,
  CpuChipIcon,
  BeakerIcon,
  ClockIcon,
  ShieldCheckIcon,
  UsersIcon,
  ArrowTrendingUpIcon,
  CodeBracketIcon
} from '@heroicons/react/24/outline'
import Link from 'next/link'

const features = [
  {
    name: 'Statistical Analysis',
    description: 'Advanced outlier detection and distribution testing with confidence intervals and statistical significance testing.',
    icon: ChartBarIcon,
    color: 'text-blue-500',
    bgColor: 'bg-blue-500/10',
    href: '/docs/2/sailfish'
  },
  {
    name: 'Machine Learning',
    description: 'ScaleFish uses ML to predict performance at different scales and estimate algorithmic complexity.',
    icon: CpuChipIcon,
    color: 'text-purple-500',
    bgColor: 'bg-purple-500/10',
    href: '/docs/2/scalefish'
  },
  {
    name: 'Regression Detection',
    description: 'SailDiff automatically compares performance between runs to catch regressions before they reach production.',
    icon: ArrowTrendingUpIcon,
    color: 'text-green-500',
    bgColor: 'bg-green-500/10',
    href: '/docs/2/saildiff'
  },
  {
    name: 'Test Lifecycle',
    description: 'Complete control over test setup and teardown with familiar attribute-based configuration.',
    icon: BeakerIcon,
    color: 'text-orange-500',
    bgColor: 'bg-orange-500/10',
    href: '/docs/1/sailfish-test-lifecycle'
  },
  {
    name: 'High Precision',
    description: 'Millisecond-scale timing with overhead estimation and in-process execution for accurate measurements.',
    icon: ClockIcon,
    color: 'text-cyan-500',
    bgColor: 'bg-cyan-500/10',
    href: '/docs/0/essential-information'
  },
  {
    name: 'Enterprise Ready',
    description: 'Team collaboration, advanced reporting, compliance features, and priority support for organizations.',
    icon: ShieldCheckIcon,
    color: 'text-red-500',
    bgColor: 'bg-red-500/10',
    href: '/enterprise'
  }
]

const codeExamples = {
  basic: `[Sailfish]
public class BasicTest
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Your performance test code
        Thread.Sleep(100);
    }
}`,
  
  variables: `[Sailfish]
public class VariableTest
{
    [SailfishVariable(10, 100, 1000)]
    public int DataSize { get; set; }

    [SailfishMethod]
    public void ProcessData()
    {
        var data = GenerateData(DataSize);
        ProcessingAlgorithm(data);
    }
}`,
  
  lifecycle: `[Sailfish]
public class LifecycleTest
{
    [SailfishGlobalSetup]
    public void GlobalSetup() { /* Once per class */ }

    [SailfishMethodSetup]
    public void MethodSetup() { /* Before each method */ }

    [SailfishMethod]
    public void TestMethod() { /* Your test */ }

    [SailfishMethodTeardown]
    public void MethodTeardown() { /* After each method */ }
}`
}

function FeatureCard({ feature }) {
  return (
    <div className="group relative overflow-hidden rounded-2xl border border-slate-200 bg-white p-8 transition-all hover:border-slate-300 hover:shadow-lg dark:border-slate-800 dark:bg-slate-900 dark:hover:border-slate-700">
      <div className="flex items-center gap-4">
        <div className={`flex h-12 w-12 items-center justify-center rounded-xl ${feature.bgColor}`}>
          <feature.icon className={`h-6 w-6 ${feature.color}`} />
        </div>
        <div>
          <h3 className="text-lg font-semibold text-slate-900 dark:text-white">
            {feature.name}
          </h3>
        </div>
      </div>
      
      <p className="mt-4 text-slate-600 dark:text-slate-400 leading-relaxed">
        {feature.description}
      </p>
      
      <Link 
        href={feature.href}
        className="mt-6 inline-flex items-center gap-2 text-sm font-medium text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
      >
        Learn more
        <svg className="h-4 w-4 transition-transform group-hover:translate-x-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
        </svg>
      </Link>
    </div>
  )
}

function CodeTabs() {
  const [activeTab, setActiveTab] = useState('basic')
  
  const tabs = [
    { id: 'basic', label: 'Basic Test', code: codeExamples.basic },
    { id: 'variables', label: 'Variables', code: codeExamples.variables },
    { id: 'lifecycle', label: 'Lifecycle', code: codeExamples.lifecycle }
  ]

  return (
    <div className="relative">
      <div className="flex space-x-1 rounded-lg bg-slate-100 p-1 dark:bg-slate-800">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`relative rounded-md px-3 py-2 text-sm font-medium transition-all ${
              activeTab === tab.id
                ? 'bg-white text-slate-900 shadow-sm dark:bg-slate-700 dark:text-white'
                : 'text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>
      
      <div className="mt-4 overflow-hidden rounded-lg bg-slate-900 ring-1 ring-slate-200 dark:ring-slate-800">
        <div className="flex items-center gap-2 border-b border-slate-700 px-4 py-3">
          <div className="flex gap-1.5">
            <div className="h-3 w-3 rounded-full bg-red-500" />
            <div className="h-3 w-3 rounded-full bg-yellow-500" />
            <div className="h-3 w-3 rounded-full bg-green-500" />
          </div>
          <div className="text-sm text-slate-400">PerformanceTest.cs</div>
        </div>
        <pre className="overflow-x-auto p-6 text-sm text-slate-300">
          <code>{tabs.find(tab => tab.id === activeTab)?.code}</code>
        </pre>
      </div>
    </div>
  )
}

export function FeaturesSection() {
  return (
    <section className="py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white sm:text-4xl">
            Everything you need for performance testing
          </h2>
          <p className="mt-6 text-lg leading-8 text-slate-600 dark:text-slate-400">
            From simple microbenchmarks to complex application testing, Sailfish provides the tools and insights you need to build faster applications.
          </p>
        </div>

        {/* Features grid */}
        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
          <div className="grid max-w-xl grid-cols-1 gap-8 lg:max-w-none lg:grid-cols-3">
            {features.map((feature) => (
              <FeatureCard key={feature.name} feature={feature} />
            ))}
          </div>
        </div>

        {/* Code examples section */}
        <div className="mx-auto mt-24 max-w-4xl">
          <div className="text-center">
            <h3 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
              Simple, familiar syntax
            </h3>
            <p className="mt-4 text-lg text-slate-600 dark:text-slate-400">
              Write performance tests using familiar C# attributes and patterns. No new languages or complex configurations required.
            </p>
          </div>
          
          <div className="mt-12">
            <CodeTabs />
          </div>
          
          <div className="mt-8 text-center">
            <Link
              href="/docs/0/quick-start"
              className="inline-flex items-center gap-2 rounded-lg bg-primary-600 px-6 py-3 text-base font-semibold text-white transition-colors hover:bg-primary-700"
            >
              <CodeBracketIcon className="h-5 w-5" />
              Try the Quick Start Guide
            </Link>
          </div>
        </div>
      </div>
    </section>
  )
}
