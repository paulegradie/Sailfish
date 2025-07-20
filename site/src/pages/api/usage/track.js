// API endpoint for tracking enterprise feature usage
import crypto from 'crypto';

// Mock database for usage tracking
const usageDatabase = new Map();

// Usage tracking utilities
const trackUsage = (licenseKey, featureName, metadata = {}) => {
  const today = new Date().toISOString().split('T')[0];
  const usageKey = `${licenseKey}_${featureName}_${today}`;
  
  const currentUsage = usageDatabase.get(usageKey) || {
    licenseKey,
    featureName,
    date: today,
    count: 0,
    sessions: []
  };

  currentUsage.count += 1;
  currentUsage.sessions.push({
    timestamp: new Date().toISOString(),
    metadata
  });

  usageDatabase.set(usageKey, currentUsage);
  
  return currentUsage;
};

const getUsageReport = (licenseKey, days = 30) => {
  const cutoff = new Date();
  cutoff.setDate(cutoff.getDate() - days);
  
  const report = {
    licenseKey,
    reportPeriod: `${cutoff.toISOString().split('T')[0]} to ${new Date().toISOString().split('T')[0]}`,
    features: {},
    totalUsage: 0
  };

  for (const [key, usage] of usageDatabase.entries()) {
    if (usage.licenseKey === licenseKey && new Date(usage.date) >= cutoff) {
      if (!report.features[usage.featureName]) {
        report.features[usage.featureName] = {
          totalCount: 0,
          dailyUsage: {}
        };
      }
      
      report.features[usage.featureName].totalCount += usage.count;
      report.features[usage.featureName].dailyUsage[usage.date] = usage.count;
      report.totalUsage += usage.count;
    }
  }

  return report;
};

export default async function handler(req, res) {
  const { method } = req;

  if (method === 'POST') {
    // Track usage
    try {
      const { licenseKey, featureName, metadata } = req.body;

      if (!licenseKey || !featureName) {
        return res.status(400).json({ 
          error: 'License key and feature name are required' 
        });
      }

      // Validate license key format
      const licensePattern = /^[A-F0-9]{8}-[A-F0-9]{8}-[A-F0-9]{8}-[A-F0-9]{8}$/;
      if (!licensePattern.test(licenseKey)) {
        return res.status(400).json({ 
          error: 'Invalid license key format' 
        });
      }

      const usage = trackUsage(licenseKey, featureName, metadata);

      res.status(200).json({
        success: true,
        usage: {
          featureName: usage.featureName,
          date: usage.date,
          count: usage.count
        },
        message: 'Usage tracked successfully'
      });

    } catch (error) {
      console.error('Usage tracking error:', error);
      res.status(500).json({ 
        error: 'Internal server error',
        message: 'Failed to track usage'
      });
    }

  } else if (method === 'GET') {
    // Get usage report
    try {
      const { licenseKey, days } = req.query;

      if (!licenseKey) {
        return res.status(400).json({ 
          error: 'License key is required' 
        });
      }

      const report = getUsageReport(licenseKey, parseInt(days) || 30);

      res.status(200).json({
        success: true,
        report,
        message: 'Usage report generated successfully'
      });

    } catch (error) {
      console.error('Usage report error:', error);
      res.status(500).json({ 
        error: 'Internal server error',
        message: 'Failed to generate usage report'
      });
    }

  } else {
    res.status(405).json({ error: 'Method not allowed' });
  }
}

// Helper functions for enterprise features
export const trackScaleFishUsage = (licenseKey, metadata = {}) => {
  return trackUsage(licenseKey, 'scalefish', {
    ...metadata,
    feature: 'complexity_analysis'
  });
};

export const trackSailDiffUsage = (licenseKey, metadata = {}) => {
  return trackUsage(licenseKey, 'saildiff', {
    ...metadata,
    feature: 'regression_analysis'
  });
};

export const trackEnterpriseFeatureUsage = (licenseKey, featureName, metadata = {}) => {
  return trackUsage(licenseKey, featureName, {
    ...metadata,
    category: 'enterprise_feature'
  });
};

// Usage analytics for enterprise dashboard
export const getEnterpriseAnalytics = (licenseKey) => {
  const report = getUsageReport(licenseKey, 30);
  
  return {
    totalUsage: report.totalUsage,
    featuresUsed: Object.keys(report.features).length,
    mostUsedFeature: Object.entries(report.features)
      .sort(([,a], [,b]) => b.totalCount - a.totalCount)[0]?.[0] || null,
    dailyAverage: Math.round(report.totalUsage / 30),
    features: report.features
  };
};
