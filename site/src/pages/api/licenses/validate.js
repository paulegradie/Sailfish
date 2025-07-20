// API endpoint for validating enterprise licenses
import crypto from 'crypto';

// Mock database - in production, use a real database
const mockLicenseDatabase = new Map();

// License validation utilities
const validateLicenseFormat = (licenseKey) => {
  const licensePattern = /^[A-F0-9]{8}-[A-F0-9]{8}-[A-F0-9]{8}-[A-F0-9]{8}$/;
  return licensePattern.test(licenseKey);
};

const checkLicenseExpiry = (expiresAt) => {
  const now = new Date();
  const expiry = new Date(expiresAt);
  return now < expiry;
};

const validateLicense = async (licenseKey) => {
  // Validate format
  if (!validateLicenseFormat(licenseKey)) {
    return {
      valid: false,
      error: 'Invalid license key format',
      code: 'INVALID_FORMAT'
    };
  }

  // Check if license exists (mock database lookup)
  const licenseData = mockLicenseDatabase.get(licenseKey);
  if (!licenseData) {
    return {
      valid: false,
      error: 'License key not found',
      code: 'NOT_FOUND'
    };
  }

  // Check if license is active
  if (licenseData.status !== 'active') {
    return {
      valid: false,
      error: 'License is not active',
      code: 'INACTIVE',
      status: licenseData.status
    };
  }

  // Check expiry
  if (!checkLicenseExpiry(licenseData.expiresAt)) {
    return {
      valid: false,
      error: 'License has expired',
      code: 'EXPIRED',
      expiresAt: licenseData.expiresAt
    };
  }

  // License is valid
  return {
    valid: true,
    license: {
      licenseKey: licenseData.licenseKey,
      licenseType: licenseData.licenseType,
      features: licenseData.features,
      customerInfo: {
        companyName: licenseData.customerInfo.companyName,
        // Don't return sensitive info
      },
      expiresAt: licenseData.expiresAt,
      status: licenseData.status
    }
  };
};

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  try {
    const { licenseKey } = req.body;

    if (!licenseKey) {
      return res.status(400).json({ 
        error: 'License key is required' 
      });
    }

    const validationResult = await validateLicense(licenseKey);

    if (validationResult.valid) {
      res.status(200).json({
        valid: true,
        license: validationResult.license,
        message: 'License is valid'
      });
    } else {
      res.status(400).json({
        valid: false,
        error: validationResult.error,
        code: validationResult.code,
        ...(validationResult.status && { status: validationResult.status }),
        ...(validationResult.expiresAt && { expiresAt: validationResult.expiresAt })
      });
    }

  } catch (error) {
    console.error('License validation error:', error);
    res.status(500).json({ 
      error: 'Internal server error',
      message: 'Failed to validate license'
    });
  }
}

// Helper function to add a license to mock database (for testing)
export const addMockLicense = (licenseData) => {
  mockLicenseDatabase.set(licenseData.licenseKey, licenseData);
};
