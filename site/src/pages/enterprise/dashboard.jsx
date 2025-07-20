import { Layout } from '@/components/Layout';
import { EnterpriseDashboard } from '@/components/EnterpriseDashboard';

export default function EnterpriseDashboardPage() {
  return (
    <Layout
      title="Enterprise Dashboard - Sailfish"
      description="Manage your Sailfish enterprise license, view usage analytics, and access enterprise features."
    >
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-8">
        <EnterpriseDashboard />
      </div>
    </Layout>
  );
}
