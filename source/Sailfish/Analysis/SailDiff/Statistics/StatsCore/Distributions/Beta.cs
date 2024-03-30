using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public static class Beta
{
    public static double Incomplete(double a, double b, double x)
    {
        if (a <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(a), "Lower limit must be greater than zero.");
        if (b <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(b), "Upper limit must be greater than zero.");
        if (x is <= 0.0 or >= 1.0)
        {
            if (x == 0.0)
                return 0.0;
            if (x == 1.0)
                return 1.0;
            throw new ArgumentOutOfRangeException(nameof(x), "Value must be between 0 and 1.");
        }

        var flag = false;
        if (b * x <= 1.0 && x <= 0.95)
            return PowerSeries(a, b, x);
        var num1 = 1.0 - x;
        double num2;
        double num3;
        double num4;
        double num5;
        if (x > a / (a + b))
        {
            flag = true;
            num2 = b;
            num3 = a;
            num4 = x;
            num5 = num1;
        }
        else
        {
            num2 = a;
            num3 = b;
            num4 = num1;
            num5 = x;
        }

        if (flag && num3 * num5 <= 1.0 && num5 <= 0.95)
        {
            var num6 = PowerSeries(num2, num3, num5);
            return num6 > 1.1102230246251565E-16 ? 1.0 - num6 : 1.0 / 1.0;
        }

        var num7 = num5 * (num2 + num3 - 2.0) - (num2 - 1.0) >= 0.0 ? Incbd(num2, num3, num5) / num4 : Incbcf(num2, num3, num5);
        var num8 = num2 * Math.Log(num5);
        var num9 = num3 * Math.Log(num4);
        if (num2 + num3 < 171.6243769563027 && Math.Abs(num8) < 709.782712893384 && Math.Abs(num9) < 709.782712893384)
        {
            var num10 = Math.Pow(num4, num3) * Math.Pow(num5, num2) / num2 * num7 * (Gamma.Function(num2 + num3) / (Gamma.Function(num2) * Gamma.Function(num3)));
            if (flag)
                num10 = num10 > 1.1102230246251565E-16 ? 1.0 - num10 : 1.0 / 1.0;
            return num10;
        }

        var d = num8 + (num9 + Gamma.Log(num2 + num3) - Gamma.Log(num2) - Gamma.Log(num3)) + Math.Log(num7 / num2);
        var num11 = d >= -745.1332191019412 ? Math.Exp(d) : 0.0;
        if (flag)
            num11 = num11 > 1.1102230246251565E-16 ? 1.0 - num11 : 1.0 / 1.0;
        return num11;
    }

    public static double Incbcf(double a, double b, double x)
    {
        var num1 = 4503599627370496.0;
        var num2 = 2.220446049250313E-16;
        var num3 = a;
        var num4 = a + b;
        var num5 = a;
        var num6 = a + 1.0;
        var num7 = 1.0;
        var num8 = b - 1.0;
        var num9 = num6;
        var num10 = a + 2.0;
        var num11 = 0.0;
        var num12 = 1.0;
        var num13 = 1.0;
        var num14 = 1.0;
        var num15 = 1.0;
        var num16 = 1.0;
        var num17 = 0;
        var num18 = 3.3306690738754696E-16;
        do
        {
            var num19 = -(x * num3 * num4) / (num5 * num6);
            var num20 = num13 + num11 * num19;
            var num21 = num14 + num12 * num19;
            var num22 = num13;
            var num24 = num14;
            var num26 = x * num7 * num8 / (num9 * num10);
            var num27 = num20 + num22 * num26;
            var num28 = num21 + num24 * num26;
            num11 = num20;
            num13 = num27;
            num12 = num21;
            num14 = num28;
            if (num28 != 0.0)
                num16 = num27 / num28;
            double num29;
            if (num16 != 0.0)
            {
                num29 = Math.Abs((num15 - num16) / num16);
                num15 = num16;
            }
            else
            {
                num29 = 1.0;
            }

            if (num29 < num18)
                return num15;
            ++num3;
            ++num4;
            num5 += 2.0;
            num6 += 2.0;
            ++num7;
            --num8;
            num9 += 2.0;
            num10 += 2.0;
            if (Math.Abs(num28) + Math.Abs(num27) > num1)
            {
                num11 *= num2;
                num13 *= num2;
                num12 *= num2;
                num14 *= num2;
            }

            if (Math.Abs(num28) < num2 || Math.Abs(num27) < num2)
            {
                num11 *= num1;
                num13 *= num1;
                num12 *= num1;
                num14 *= num1;
            }
        } while (++num17 < 300);

        return num15;
    }

    public static double Incbd(double a, double b, double x)
    {
        var num1 = 4503599627370496.0;
        var num2 = 2.220446049250313E-16;
        var num3 = a;
        var num4 = b - 1.0;
        var num5 = a;
        var num6 = a + 1.0;
        var num7 = 1.0;
        var num8 = a + b;
        var num9 = a + 1.0;
        var num10 = a + 2.0;
        var num11 = 0.0;
        var num12 = 1.0;
        var num13 = 1.0;
        var num14 = 1.0;
        var num15 = x / (1.0 - x);
        var num16 = 1.0;
        var num17 = 1.0;
        var num18 = 0;
        var num19 = 3.3306690738754696E-16;
        do
        {
            var num20 = -(num15 * num3 * num4) / (num5 * num6);
            var num21 = num13 + num11 * num20;
            var num22 = num14 + num12 * num20;
            var num23 = num13;
            var num25 = num14;
            var num27 = num15 * num7 * num8 / (num9 * num10);
            var num28 = num21 + num23 * num27;
            var num29 = num22 + num25 * num27;
            num11 = num21;
            num13 = num28;
            num12 = num22;
            num14 = num29;
            if (num29 != 0.0)
                num17 = num28 / num29;
            double num30;
            if (num17 != 0.0)
            {
                num30 = Math.Abs((num16 - num17) / num17);
                num16 = num17;
            }
            else
            {
                num30 = 1.0;
            }

            if (num30 < num19)
                return num16;
            ++num3;
            --num4;
            num5 += 2.0;
            num6 += 2.0;
            ++num7;
            ++num8;
            num9 += 2.0;
            num10 += 2.0;
            if (Math.Abs(num29) + Math.Abs(num28) > num1)
            {
                num11 *= num2;
                num13 *= num2;
                num12 *= num2;
                num14 *= num2;
            }

            if (Math.Abs(num29) < num2 || Math.Abs(num28) < num2)
            {
                num11 *= num1;
                num13 *= num1;
                num12 *= num1;
                num14 *= num1;
            }
        } while (++num18 < 300);

        return num16;
    }

    public static double IncompleteInverse(double aa, double bb, double yy0)
    {
        if (yy0 <= 0.0)
            return 0.0;
        if (yy0 >= 1.0)
            return 1.0;
        bool flag1;
        double num1;
        bool flag2;
        double num2;
        double num3;
        double num4;
        double x;
        double num5;
        double num6;
        if (aa <= 1.0 || bb <= 1.0)
        {
            flag1 = true;
            num1 = 4.440892098500626E-16;
            flag2 = false;
            num2 = aa;
            num3 = bb;
            num4 = yy0;
            x = num2 / (num2 + num3);
            num5 = Incomplete(num2, num3, x);
        }
        else
        {
            flag1 = false;
            num1 = 0.0001;
            var num7 = -Normal.Inverse(yy0);
            if (yy0 > 0.5)
            {
                flag2 = true;
                num2 = bb;
                num3 = aa;
                num4 = 1.0 - yy0;
                num7 = -num7;
            }
            else
            {
                flag2 = false;
                num2 = aa;
                num3 = bb;
                num4 = yy0;
            }

            var num8 = (num7 * num7 - 3.0) / 6.0;
            num6 = 2.0 / (1.0 / (2.0 * num2 - 1.0) + 1.0 / (2.0 * num3 - 1.0));
            var d = 2.0 * (num7 * Math.Sqrt(num6 + num8) / num6 - (1.0 / (2.0 * num3 - 1.0) - 1.0 / (2.0 * num2 - 1.0)) * (num8 + 5.0 / 6.0 - 2.0 / (3.0 * num6)));
            if (d < -745.1332191019412)
                throw new ArithmeticException("underflow");
            x = num2 / (num2 + num3 * Math.Exp(d));
            num5 = Incomplete(num2, num3, x);
            if (Math.Abs((num5 - num4) / num4) < 0.01)
                goto label_39;
        }

        label_12:
        num6 = 0.0;
        var num9 = 0.0;
        var num10 = 1.0;
        var num11 = 1.0;
        var num12 = 0.5;
        var num13 = 0;
        for (var index = 0; index < 400; ++index)
        {
            if (index != 0)
            {
                x = num6 + num12 * (num10 - num6);
                if (x == 1.0)
                    x = 1.0 / 1.0;
                num5 = Incomplete(num2, num3, x);
                if (Math.Abs((num10 - num6) / (num10 + num6)) < num1)
                {
                    num6 = x;
                    goto label_39;
                }
            }

            if (num5 < num4)
            {
                num6 = x;
                num9 = num5;
                if (num13 < 0)
                {
                    num13 = 0;
                    num12 = 0.5;
                }
                else
                {
                    num12 = num13 <= 1 ? (num4 - num5) / (num11 - num9) : 0.5 * num12 + 0.5;
                }

                ++num13;
                if (num6 > 0.75)
                {
                    if (flag2)
                    {
                        flag2 = false;
                        num2 = aa;
                        num3 = bb;
                        num4 = yy0;
                    }
                    else
                    {
                        flag2 = true;
                        num2 = bb;
                        num3 = aa;
                        num4 = 1.0 - yy0;
                    }

                    x = 1.0 - x;
                    num5 = Incomplete(num2, num3, x);
                    goto label_12;
                }
            }
            else
            {
                num10 = x;
                if (flag2 && num10 < 1.1102230246251565E-16)
                {
                    num6 = 0.0;
                    goto label_52;
                }

                num11 = num5;
                if (num13 > 0)
                {
                    num13 = 0;
                    num12 = 0.5;
                }
                else
                {
                    num12 = num13 >= -1 ? (num5 - num4) / (num11 - num9) : 0.5 * num12;
                }

                --num13;
            }
        }

        if (num6 >= 1.0)
        {
            num6 = 1.0 / 1.0;
            goto label_52;
        }

        if (x == 0.0) throw new ArithmeticException("underflow");

        label_39:
        if (!flag1)
        {
            num6 = x;
            var num14 = Gamma.Log(num2 + num3) - Gamma.Log(num2) - Gamma.Log(num3);
            for (var index = 0; index < 10; ++index)
            {
                if (index != 0)
                    num5 = Incomplete(num2, num3, num6);
                var d = (num2 - 1.0) * Math.Log(num6) + (num3 - 1.0) * Math.Log(1.0 - num6) + num14;
                var num15 = d >= -745.1332191019412 ? Math.Exp(d) : throw new ArithmeticException("underflow");
                var num16 = (num5 - num4) / num15;
                num6 -= num16;
                if (num6 <= 0.0)
                    throw new ArithmeticException("underflow");
                if (num6 >= 1.0)
                {
                    num6 = 1.0 / 1.0;
                    break;
                }

                if (Math.Abs(num16 / num6) < 7.105427357601002E-15)
                    break;
            }
        }

        label_52:
        if (flag2)
            num6 = num6 > double.Epsilon ? 1.0 - num6 : 1.0;
        return num6;
    }

    private static double PowerSeries(double a, double b, double x)
    {
        var num1 = 1.0 / a;
        var num2 = (1.0 - b) * x;
        var num3 = num2 / (a + 1.0);
        var num4 = num3;
        var num5 = num2;
        var num6 = 2.0;
        var num7 = 0.0;
        var num8 = 1.1102230246251565E-16 * num1;
        while (Math.Abs(num3) > num8)
        {
            var num9 = (num6 - b) * x / num6;
            num5 *= num9;
            num3 = num5 / (a + num6);
            num7 += num3;
            ++num6;
        }

        var d1 = num7 + num4 + num1;
        var num10 = a * Math.Log(x);
        double num11;
        if (a + b < 171.6243769563027 && Math.Abs(num10) < 709.782712893384)
        {
            var num12 = Gamma.Function(a + b) / (Gamma.Function(a) * Gamma.Function(b));
            num11 = d1 * num12 * Math.Pow(x, a);
        }
        else
        {
            var d2 = Gamma.Log(a + b) - Gamma.Log(a) - Gamma.Log(b) + num10 + Math.Log(d1);
            num11 = d2 >= -745.1332191019412 ? Math.Exp(d2) : 0.0;
        }

        return num11;
    }
}