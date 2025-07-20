import { Layout } from '@/components/Layout';
import { LicenseManager } from '@/components/LicenseManager';

export default function LicenseManagerPage() {
  return (
    <Layout
      title="License Manager"
      description="Validate and manage Sailfish enterprise licenses"
    >
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-12">
        <LicenseManager />
      </div>
    </Layout>
  );
}
