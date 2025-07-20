import { useState, useEffect } from 'react';
import { Card } from './ui/Card';
import { Badge } from './ui/Badge';
import { Button } from './ui/Button';

export function EnterpriseDashboard() {
  const [licenseInfo, setLicenseInfo] = useState(null);
  const [usageStats, setUsageStats] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    setIsLoading(true);
    try {
      // Mock data - replace with actual API calls
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      setLicenseInfo({
        licenseKey: 'ABCD1234-EFGH5678-IJKL9012-MNOP3456',
        companyName: 'Acme Corporation',
        licenseType: 'enterprise',
        status: 'active',
        expiresAt: '2025-12-31T23:59:59Z',
        features: {
          scalefish: true,
          saildiff: true,
          prioritySupport: true,
          enterpriseFeatures: true,
          maxUsers: -1
        }
      });

      setUsageStats({
        totalTests: 1247,
        testsThisMonth: 89,
        avgExecutionTime: '2.3s',
        performanceImprovement: '34%',
        activeUsers: 12,
        recentActivity: [
          { date: '2024-12-20', action: 'Performance test executed', user: 'john.doe@acme.com', test: 'API Response Time Test' },
          { date: '2024-12-19', action: 'SailDiff analysis completed', user: 'jane.smith@acme.com', test: 'Database Query Performance' },
          { date: '2024-12-18', action: 'ScaleFish complexity analysis', user: 'bob.wilson@acme.com', test: 'Algorithm Optimization Test' },
          { date: '2024-12-17', action: 'New test suite created', user: 'alice.brown@acme.com', test: 'Microservice Load Test' }
        ]
      });
    } catch (error) {
      console.error('Failed to load dashboard data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto p-6">
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 rounded w-1/3"></div>
          <div className="grid md:grid-cols-3 gap-6">
            {[1, 2, 3].map(i => (
              <div key={i} className="h-32 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto p-6 space-y-8">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            Enterprise Dashboard
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Welcome back, {licenseInfo?.companyName}
          </p>
        </div>
        <Badge variant={licenseInfo?.status === 'active' ? 'success' : 'destructive'}>
          {licenseInfo?.status?.toUpperCase()}
        </Badge>
      </div>

      {/* License Information */}
      <Card className="p-6">
        <h2 className="text-xl font-semibold mb-4">License Information</h2>
        <div className="grid md:grid-cols-2 gap-6">
          <div>
            <div className="space-y-3">
              <div>
                <span className="text-sm font-medium text-gray-500">License Key</span>
                <p className="font-mono text-sm bg-gray-100 dark:bg-gray-800 p-2 rounded">
                  {licenseInfo?.licenseKey}
                </p>
              </div>
              <div>
                <span className="text-sm font-medium text-gray-500">Company</span>
                <p className="text-lg font-medium">{licenseInfo?.companyName}</p>
              </div>
              <div>
                <span className="text-sm font-medium text-gray-500">License Type</span>
                <p className="text-lg font-medium capitalize">{licenseInfo?.licenseType}</p>
              </div>
            </div>
          </div>
          <div>
            <div className="space-y-3">
              <div>
                <span className="text-sm font-medium text-gray-500">Expires</span>
                <p className="text-lg font-medium">
                  {new Date(licenseInfo?.expiresAt).toLocaleDateString()}
                </p>
              </div>
              <div>
                <span className="text-sm font-medium text-gray-500">Max Users</span>
                <p className="text-lg font-medium">
                  {licenseInfo?.features?.maxUsers === -1 ? 'Unlimited' : licenseInfo?.features?.maxUsers}
                </p>
              </div>
              <div>
                <span className="text-sm font-medium text-gray-500">Features</span>
                <div className="flex flex-wrap gap-2 mt-1">
                  {Object.entries(licenseInfo?.features || {}).map(([feature, enabled]) => (
                    <Badge 
                      key={feature} 
                      variant={enabled ? 'success' : 'secondary'}
                      className="text-xs"
                    >
                      {feature}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </Card>

      {/* Usage Statistics */}
      <div className="grid md:grid-cols-4 gap-6">
        <Card className="p-6 text-center">
          <div className="text-3xl font-bold text-blue-600 dark:text-blue-400">
            {usageStats?.totalTests}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Total Tests
          </div>
        </Card>
        <Card className="p-6 text-center">
          <div className="text-3xl font-bold text-green-600 dark:text-green-400">
            {usageStats?.testsThisMonth}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Tests This Month
          </div>
        </Card>
        <Card className="p-6 text-center">
          <div className="text-3xl font-bold text-purple-600 dark:text-purple-400">
            {usageStats?.avgExecutionTime}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Avg Execution Time
          </div>
        </Card>
        <Card className="p-6 text-center">
          <div className="text-3xl font-bold text-orange-600 dark:text-orange-400">
            {usageStats?.performanceImprovement}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Performance Gain
          </div>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card className="p-6">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-xl font-semibold">Recent Activity</h2>
          <Button variant="outline" size="sm">
            View All
          </Button>
        </div>
        <div className="space-y-4">
          {usageStats?.recentActivity?.map((activity, index) => (
            <div key={index} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <div>
                <p className="font-medium text-gray-900 dark:text-white">
                  {activity.action}
                </p>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  {activity.test} • {activity.user}
                </p>
              </div>
              <div className="text-sm text-gray-500">
                {new Date(activity.date).toLocaleDateString()}
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Quick Actions */}
      <Card className="p-6">
        <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
        <div className="grid md:grid-cols-3 gap-4">
          <Button className="h-16 flex flex-col items-center justify-center">
            <span className="text-lg mb-1">📊</span>
            <span>View Analytics</span>
          </Button>
          <Button variant="outline" className="h-16 flex flex-col items-center justify-center">
            <span className="text-lg mb-1">👥</span>
            <span>Manage Team</span>
          </Button>
          <Button variant="outline" className="h-16 flex flex-col items-center justify-center">
            <span className="text-lg mb-1">⚙️</span>
            <span>Settings</span>
          </Button>
        </div>
      </Card>
    </div>
  );
}
