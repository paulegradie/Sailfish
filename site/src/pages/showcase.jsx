import { Layout } from '@/components/Layout'
import { Logo, LogoIcon, LogoMark } from '@/components/ui/Logo'
import { Breadcrumb, CompactBreadcrumb } from '@/components/ui/Breadcrumb'

export default function ComponentShowcase() {
  return (
    <Layout title="Component Showcase">
      <div className="space-y-12">
        {/* Logo Components */}
        <section>
          <h2 className="text-2xl font-bold mb-6">Logo Components</h2>
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-semibold mb-3">Logo Variants</h3>
              <div className="flex flex-wrap items-center gap-8 p-6 bg-slate-50 dark:bg-slate-800 rounded-lg">
                <Logo size="sm" />
                <Logo size="md" />
                <Logo size="lg" />
                <Logo size="xl" />
              </div>
            </div>

            <div>
              <h3 className="text-lg font-semibold mb-3">Logo Colors</h3>
              <div className="flex flex-wrap items-center gap-8 p-6 bg-slate-50 dark:bg-slate-800 rounded-lg">
                <Logo variant="default" />
                <Logo variant="primary" />
                <div className="bg-slate-900 p-4 rounded">
                  <Logo variant="light" />
                </div>
                <div className="bg-white p-4 rounded">
                  <Logo variant="dark" />
                </div>
              </div>
            </div>

            <div>
              <h3 className="text-lg font-semibold mb-3">Logo Icon & Mark</h3>
              <div className="flex flex-wrap items-center gap-8 p-6 bg-slate-50 dark:bg-slate-800 rounded-lg">
                <LogoIcon size="sm" />
                <LogoIcon size="md" />
                <LogoIcon size="lg" />
                <LogoMark />
              </div>
            </div>
          </div>
        </section>

        {/* Navigation Components */}
        <section>
          <h2 className="text-2xl font-bold mb-6">Navigation Components</h2>
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-semibold mb-3">Breadcrumb Navigation</h3>
              <div className="space-y-4 p-6 bg-slate-50 dark:bg-slate-800 rounded-lg">
                <div>
                  <p className="text-sm text-slate-600 dark:text-slate-400 mb-2">Full Breadcrumb:</p>
                  <Breadcrumb />
                </div>
                <div>
                  <p className="text-sm text-slate-600 dark:text-slate-400 mb-2">Compact Breadcrumb:</p>
                  <CompactBreadcrumb />
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Design System Colors */}
        <section>
          <h2 className="text-2xl font-bold mb-6">Design System Colors</h2>
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-semibold mb-3">Primary Colors</h3>
              <div className="grid grid-cols-10 gap-2">
                {[50, 100, 200, 300, 400, 500, 600, 700, 800, 900].map((shade) => (
                  <div key={shade} className="text-center">
                    <div
                      className={`h-12 w-full rounded mb-2 bg-primary-${shade}`}
                    />
                    <span className="text-xs">{shade}</span>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <h3 className="text-lg font-semibold mb-3">Gray Colors</h3>
              <div className="grid grid-cols-10 gap-2">
                {[50, 100, 200, 300, 400, 500, 600, 700, 800, 900].map((shade) => (
                  <div key={shade} className="text-center">
                    <div
                      className={`h-12 w-full rounded mb-2 bg-gray-${shade}`}
                    />
                    <span className="text-xs">{shade}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>
      </div>
    </Layout>
  )
}
