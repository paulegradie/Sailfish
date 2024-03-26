using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;

[Serializable]
internal sealed class SingularValueDecomposition : ICloneable
{
    private double[,] diagonalMatrix;
    private double? lnpseudoDeterminant;
    private int m;
    private int n;
    private bool swapped;


    public SingularValueDecomposition(
        double[,] value,
        bool computeLeftSingularVectors,
        bool computeRightSingularVectors,
        bool autoTranspose)
        : this(value, computeLeftSingularVectors, computeRightSingularVectors, autoTranspose, false)
    {
    }

    public SingularValueDecomposition(
        double[,]? value,
        bool computeLeftSingularVectors,
        bool computeRightSingularVectors,
        bool autoTranspose,
        bool inPlace)
    {
        m = value?.Rows() ?? throw new ArgumentNullException(nameof(value), "Matrix cannot be null.");
        if (m == 0)
            throw new ArgumentException("Matrix does not have any rows.", nameof(value));
        n = value.Columns();
        if (n == 0)
            throw new ArgumentException("Matrix does not have any columns.", nameof(value));
        double[,] matrix;
        if (m < n)
        {
            if (!autoTranspose)
            {
                matrix = inPlace ? value : value.Copy();
            }
            else
            {
                matrix = value.Transpose(inPlace && m == n);
                n = value.Rows();
                m = value.Columns();
                swapped = true;
                var num = computeLeftSingularVectors ? 1 : 0;
                computeLeftSingularVectors = computeRightSingularVectors;
                computeRightSingularVectors = num != 0;
            }
        }
        else
        {
            matrix = inPlace ? value : value.Copy();
        }

        var length1 = Math.Min(m, n);
        var length2 = Math.Min(m + 1, n);
        Diagonal = new double[length2];
        LeftSingularVectors = new double[m, length1];
        RightSingularVectors = new double[n, n];
        var vector1 = new double[n];
        var vector2 = new double[m];
        var flag1 = computeLeftSingularVectors;
        var flag2 = computeRightSingularVectors;
        Ordering = new int[length2];
        for (var index = 0; index < length2; ++index)
            Ordering[index] = index;
        var val1 = Math.Min(m - 1, n);
        var val2 = Math.Max(0, Math.Min(n - 2, m));
        var num1 = Math.Max(val1, val2);
        for (var index1 = 0; index1 < num1; ++index1)
        {
            if (index1 < val1)
            {
                Diagonal[index1] = 0.0;
                for (var index2 = index1; index2 < matrix.Rows(); ++index2)
                    Diagonal[index1] = Tools.Hypotenuse(Diagonal[index1], matrix[index2, index1]);
                if (Diagonal[index1] != 0.0)
                {
                    if (matrix[index1, index1] < 0.0)
                        Diagonal[index1] = -Diagonal[index1];
                    for (var index3 = index1; index3 < matrix.Rows(); ++index3)
                        matrix[index3, index1] /= Diagonal[index1];
                    ++matrix[index1, index1];
                }

                Diagonal[index1] = -Diagonal[index1];
            }

            for (var index4 = index1 + 1; index4 < n; ++index4)
            {
                if ((index1 < val1) & (Diagonal[index1] != 0.0))
                {
                    var num2 = 0.0;
                    for (var index5 = index1; index5 < matrix.Rows(); ++index5)
                        num2 += matrix[index5, index1] * matrix[index5, index4];
                    var num3 = -num2 / matrix[index1, index1];
                    for (var index6 = index1; index6 < matrix.Rows(); ++index6)
                        matrix[index6, index4] += num3 * matrix[index6, index1];
                }

                vector1[index4] = matrix[index1, index4];
            }

            if (flag1 & (index1 < val1))
                for (var index7 = index1; index7 < matrix.Rows(); ++index7)
                    LeftSingularVectors[index7, index1] = matrix[index7, index1];
            if (index1 < val2)
            {
                vector1[index1] = 0.0;
                for (var index8 = index1 + 1; index8 < vector1.Rows(); ++index8)
                    vector1[index1] = Tools.Hypotenuse(vector1[index1], vector1[index8]);
                if (vector1[index1] != 0.0)
                {
                    if (vector1[index1 + 1] < 0.0)
                        vector1[index1] = -vector1[index1];
                    for (var index9 = index1 + 1; index9 < vector1.Rows(); ++index9)
                        vector1[index9] /= vector1[index1];
                    ++vector1[index1 + 1];
                }

                vector1[index1] = -vector1[index1];
                if ((index1 + 1 < m) & (vector1[index1] != 0.0))
                {
                    for (var index10 = index1 + 1; index10 < vector2.Rows(); ++index10)
                        vector2[index10] = 0.0;
                    for (var index11 = index1 + 1; index11 < matrix.Rows(); ++index11)
                        for (var index12 = index1 + 1; index12 < matrix.Columns(); ++index12)
                            vector2[index11] += vector1[index12] * matrix[index11, index12];
                    for (var index13 = index1 + 1; index13 < n; ++index13)
                    {
                        var num4 = -vector1[index13] / vector1[index1 + 1];
                        for (var index14 = index1 + 1; index14 < vector2.Rows(); ++index14)
                            matrix[index14, index13] += num4 * vector2[index14];
                    }
                }

                if (flag2)
                    for (var index15 = index1 + 1; index15 < RightSingularVectors.Rows(); ++index15)
                        RightSingularVectors[index15, index1] = vector1[index15];
            }
        }

        var num5 = Math.Min(n, m + 1);
        if (val1 < n)
            Diagonal[val1] = matrix[val1, val1];
        if (m < num5)
            Diagonal[num5 - 1] = 0.0;
        if (val2 + 1 < num5)
            vector1[val2] = matrix[val2, num5 - 1];
        vector1[num5 - 1] = 0.0;
        if (flag1)
        {
            for (var index16 = val1; index16 < length1; ++index16)
            {
                for (var index17 = 0; index17 < LeftSingularVectors.Rows(); ++index17)
                    LeftSingularVectors[index17, index16] = 0.0;
                LeftSingularVectors[index16, index16] = 1.0;
            }

            for (var index18 = val1 - 1; index18 >= 0; --index18)
                if (Diagonal[index18] != 0.0)
                {
                    for (var index19 = index18 + 1; index19 < length1; ++index19)
                    {
                        var num6 = 0.0;
                        for (var index20 = index18; index20 < LeftSingularVectors.Rows(); ++index20)
                            num6 += LeftSingularVectors[index20, index18] * LeftSingularVectors[index20, index19];
                        var num7 = -num6 / LeftSingularVectors[index18, index18];
                        for (var index21 = index18; index21 < LeftSingularVectors.Rows(); ++index21)
                            LeftSingularVectors[index21, index19] += num7 * LeftSingularVectors[index21, index18];
                    }

                    for (var index22 = index18; index22 < LeftSingularVectors.Rows(); ++index22)
                        LeftSingularVectors[index22, index18] = -LeftSingularVectors[index22, index18];
                    LeftSingularVectors[index18, index18] = 1.0 + LeftSingularVectors[index18, index18];
                    for (var index23 = 0; index23 < index18 - 1; ++index23)
                        LeftSingularVectors[index23, index18] = 0.0;
                }
                else
                {
                    for (var index24 = 0; index24 < LeftSingularVectors.Rows(); ++index24)
                        LeftSingularVectors[index24, index18] = 0.0;
                    LeftSingularVectors[index18, index18] = 1.0;
                }
        }

        if (flag2)
            for (var index25 = n - 1; index25 >= 0; --index25)
            {
                if ((index25 < val2) & (vector1[index25] != 0.0))
                    for (var index26 = index25 + 1; index26 < n; ++index26)
                    {
                        var num8 = 0.0;
                        for (var index27 = index25 + 1; index27 < RightSingularVectors.Rows(); ++index27)
                            num8 += RightSingularVectors[index27, index25] * RightSingularVectors[index27, index26];
                        var num9 = -num8 / RightSingularVectors[index25 + 1, index25];
                        for (var index28 = index25 + 1; index28 < RightSingularVectors.Rows(); ++index28)
                            RightSingularVectors[index28, index26] += num9 * RightSingularVectors[index28, index25];
                    }

                for (var index29 = 0; index29 < RightSingularVectors.Rows(); ++index29)
                    RightSingularVectors[index29, index25] = 0.0;
                RightSingularVectors[index25, index25] = 1.0;
            }

        var num10 = num5 - 1;
        const double num12 = 1.1102230246251565E-16;
        while (num5 > 0)
        {
            int index30;
            for (index30 = num5 - 2; index30 >= -1 && index30 != -1; --index30)
            {
                var num13 = 1.49322178960515E-300 + num12 * (Math.Abs(Diagonal[index30]) + Math.Abs(Diagonal[index30 + 1]));
                if (Math.Abs(vector1[index30]) <= num13 || double.IsNaN(vector1[index30]))
                {
                    vector1[index30] = 0.0;
                    break;
                }
            }

            int num14;
            if (index30 == num5 - 2)
            {
                num14 = 4;
            }
            else
            {
                int index31;
                for (index31 = num5 - 1; index31 >= index30 && index31 != index30; --index31)
                {
                    var num15 = (index31 != num5 ? Math.Abs(vector1[index31]) : 0.0) + (index31 != index30 + 1 ? Math.Abs(vector1[index31 - 1]) : 0.0);
                    if (Math.Abs(Diagonal[index31]) <= num12 * num15)
                    {
                        Diagonal[index31] = 0.0;
                        break;
                    }
                }

                if (index31 == index30)
                {
                    num14 = 3;
                }
                else if (index31 == num5 - 1)
                {
                    num14 = 1;
                }
                else
                {
                    num14 = 2;
                    index30 = index31;
                }
            }

            var index32 = index30 + 1;
            switch (num14)
            {
                case 1:
                    var b1 = vector1[num5 - 2];
                    vector1[num5 - 2] = 0.0;
                    for (var index33 = num5 - 2; index33 >= index32; --index33)
                    {
                        var num16 = Tools.Hypotenuse(Diagonal[index33], b1);
                        var num17 = Diagonal[index33] / num16;
                        var num18 = b1 / num16;
                        Diagonal[index33] = num16;
                        if (index33 != index32)
                        {
                            b1 = -num18 * vector1[index33 - 1];
                            vector1[index33 - 1] = num17 * vector1[index33 - 1];
                        }

                        if (flag2)
                            for (var index34 = 0; index34 < RightSingularVectors.Rows(); ++index34)
                            {
                                var num19 = num17 * RightSingularVectors[index34, index33] + num18 * RightSingularVectors[index34, num5 - 1];
                                RightSingularVectors[index34, num5 - 1] = -num18 * RightSingularVectors[index34, index33] + num17 * RightSingularVectors[index34, num5 - 1];
                                RightSingularVectors[index34, index33] = num19;
                            }
                    }

                    continue;
                case 2:
                    var b2 = vector1[index32 - 1];
                    vector1[index32 - 1] = 0.0;
                    for (var index35 = index32; index35 < num5; ++index35)
                    {
                        var num20 = Tools.Hypotenuse(Diagonal[index35], b2);
                        var num21 = Diagonal[index35] / num20;
                        var num22 = b2 / num20;
                        Diagonal[index35] = num20;
                        b2 = -num22 * vector1[index35];
                        vector1[index35] = num21 * vector1[index35];
                        if (flag1)
                            for (var index36 = 0; index36 < LeftSingularVectors.Rows(); ++index36)
                            {
                                var num23 = num21 * LeftSingularVectors[index36, index35] + num22 * LeftSingularVectors[index36, index32 - 1];
                                LeftSingularVectors[index36, index32 - 1] = -num22 * LeftSingularVectors[index36, index35] + num21 * LeftSingularVectors[index36, index32 - 1];
                                LeftSingularVectors[index36, index35] = num23;
                            }
                    }

                    continue;
                case 3:
                    var num24 = Math.Max(
                        Math.Max(Math.Max(Math.Max(Math.Abs(Diagonal[num5 - 1]), Math.Abs(Diagonal[num5 - 2])), Math.Abs(vector1[num5 - 2])), Math.Abs(Diagonal[index32])),
                        Math.Abs(vector1[index32]));
                    var num25 = Diagonal[num5 - 1] / num24;
                    var num26 = Diagonal[num5 - 2] / num24;
                    var num27 = vector1[num5 - 2] / num24;
                    var num28 = Diagonal[index32] / num24;
                    var num29 = vector1[index32] / num24;
                    var num30 = ((num26 + num25) * (num26 - num25) + num27 * num27) / 2.0;
                    var num31 = num25 * num27 * (num25 * num27);
                    var num32 = 0.0;
                    if (num30 != 0.0 || num31 != 0.0)
                    {
                        var num33 = num30 >= 0.0 ? Math.Sqrt(num30 * num30 + num31) : -Math.Sqrt(num30 * num30 + num31);
                        num32 = num31 / (num30 + num33);
                    }

                    var a1 = (num28 + num25) * (num28 - num25) + num32;
                    var b3 = num28 * num29;
                    for (var index37 = index32; index37 < num5 - 1; ++index37)
                    {
                        var num34 = Tools.Hypotenuse(a1, b3);
                        var num35 = a1 / num34;
                        var num36 = b3 / num34;
                        if (index37 != index32)
                            vector1[index37 - 1] = num34;
                        var a2 = num35 * Diagonal[index37] + num36 * vector1[index37];
                        vector1[index37] = num35 * vector1[index37] - num36 * Diagonal[index37];
                        var b4 = num36 * Diagonal[index37 + 1];
                        Diagonal[index37 + 1] = num35 * Diagonal[index37 + 1];
                        if (flag2)
                            for (var index38 = 0; index38 < RightSingularVectors.Rows(); ++index38)
                            {
                                var num37 = num35 * RightSingularVectors[index38, index37] + num36 * RightSingularVectors[index38, index37 + 1];
                                RightSingularVectors[index38, index37 + 1] = -num36 * RightSingularVectors[index38, index37] + num35 * RightSingularVectors[index38, index37 + 1];
                                RightSingularVectors[index38, index37] = num37;
                            }

                        var num38 = Tools.Hypotenuse(a2, b4);
                        var num39 = a2 / num38;
                        var num40 = b4 / num38;
                        Diagonal[index37] = num38;
                        a1 = num39 * vector1[index37] + num40 * Diagonal[index37 + 1];
                        Diagonal[index37 + 1] = -num40 * vector1[index37] + num39 * Diagonal[index37 + 1];
                        b3 = num40 * vector1[index37 + 1];
                        vector1[index37 + 1] = num39 * vector1[index37 + 1];
                        if (!flag1 || index37 >= m - 1) continue;
                        for (var index39 = 0; index39 < LeftSingularVectors.Rows(); ++index39)
                        {
                            var num41 = num39 * LeftSingularVectors[index39, index37] + num40 * LeftSingularVectors[index39, index37 + 1];
                            LeftSingularVectors[index39, index37 + 1] = -num40 * LeftSingularVectors[index39, index37] + num39 * LeftSingularVectors[index39, index37 + 1];
                            LeftSingularVectors[index39, index37] = num41;
                        }
                    }

                    vector1[num5 - 2] = a1;
                    continue;
                case 4:
                    if (Diagonal[index32] <= 0.0)
                    {
                        Diagonal[index32] = Diagonal[index32] < 0.0 ? -Diagonal[index32] : 0.0;
                        if (flag2)
                            for (var index40 = 0; index40 <= num10; ++index40)
                                RightSingularVectors[index40, index32] = -RightSingularVectors[index40, index32];
                    }

                    for (; index32 < num10 && Diagonal[index32] < Diagonal[index32 + 1]; ++index32)
                    {
                        (Diagonal[index32], Diagonal[index32 + 1]) = (Diagonal[index32 + 1], Diagonal[index32]);
                        if (flag2 && index32 < n - 1)
                            for (var index41 = 0; index41 < n; ++index41)
                            {
                                (RightSingularVectors[index41, index32 + 1], RightSingularVectors[index41, index32]) = (RightSingularVectors[index41, index32], RightSingularVectors[index41, index32 + 1]);
                            }

                        if (!flag1 || index32 >= m - 1) continue;
                        for (var index42 = 0; index42 < LeftSingularVectors.Rows(); ++index42)
                        {
                            (LeftSingularVectors[index42, index32 + 1], LeftSingularVectors[index42, index32]) = (LeftSingularVectors[index42, index32], LeftSingularVectors[index42, index32 + 1]);
                        }
                    }

                    --num5;
                    continue;
                default:
                    continue;
            }
        }

        if (!swapped)
            return;
        (LeftSingularVectors, RightSingularVectors) = (RightSingularVectors, LeftSingularVectors);
    }

    private SingularValueDecomposition()
    {
    }

    public double Threshold => 1.1102230246251565E-16 * Math.Max(m, n) * Diagonal[0];

    public double[] Diagonal { get; private set; }

    public double[,] DiagonalMatrix => diagonalMatrix != null
        ? diagonalMatrix
        : diagonalMatrix = InternalOps.Diagonal(LeftSingularVectors.Columns(), RightSingularVectors.Columns(), Diagonal);

    public double[,] RightSingularVectors { get; private set; }

    public double[,] LeftSingularVectors { get; private set; }

    public int[] Ordering { get; private set; }

    public object Clone()
    {
        var valueDecomposition = new SingularValueDecomposition();
        valueDecomposition.m = m;
        valueDecomposition.n = n;
        valueDecomposition.Diagonal = (double[])Diagonal.Clone();
        valueDecomposition.Ordering = (int[])Ordering.Clone();
        valueDecomposition.swapped = swapped;
        if (LeftSingularVectors != null)
            valueDecomposition.LeftSingularVectors = LeftSingularVectors.MemberwiseClone<double>();
        if (RightSingularVectors != null)
            valueDecomposition.RightSingularVectors = RightSingularVectors.MemberwiseClone<double>();
        return valueDecomposition;
    }

    public double[,] Solve(double[,] value)
    {
        var b1 = value;
        var threshold = Threshold;
        var length1 = Diagonal.Rows();
        var b2 = new double[length1, length1];
        for (var index = 0; index < Diagonal.Rows(); ++index)
            b2[index, index] = Math.Abs(Diagonal[index]) > threshold ? 1.0 / Diagonal[index] : 0.0;
        var numArray = RightSingularVectors.Dot(b2);
        var length2 = RightSingularVectors.Rows();
        var length3 = LeftSingularVectors.Rows();
        var num1 = LeftSingularVectors.Columns();
        var a = new double[length2, length3];
        for (var index1 = 0; index1 < length2; ++index1)
            for (var index2 = 0; index2 < length3; ++index2)
            {
                var num2 = 0.0;
                for (var index3 = 0; index3 < num1; ++index3)
                    num2 += numArray[index1, index3] * LeftSingularVectors[index2, index3];
                a[index1, index2] = num2;
            }

        return a.Dot(b1);
    }

    public double[] Solve(double[] value)
    {
        var threshold = Threshold;
        var columnVector = value;
        var length1 = Diagonal.Rows();
        var b = new double[length1, length1];
        for (var index = 0; index < Diagonal.Rows(); ++index)
            b[index, index] = Math.Abs(Diagonal[index]) > threshold ? 1.0 / Diagonal[index] : 0.0;
        var numArray = RightSingularVectors.Dot(b);
        var length2 = LeftSingularVectors.Rows();
        var length3 = RightSingularVectors.Rows();
        var a = new double[length3, length2];
        for (var index1 = 0; index1 < length3; ++index1)
            for (var index2 = 0; index2 < length2; ++index2)
            {
                var num = 0.0;
                for (var index3 = 0; index3 < length1; ++index3)
                    num += numArray[index1, index3] * LeftSingularVectors[index2, index3];
                a[index1, index2] = num;
            }

        return a.Dot(columnVector);
    }

    public double[,] Inverse()
    {
        var threshold = Threshold;
        var length1 = RightSingularVectors.Rows();
        var num1 = RightSingularVectors.Columns();
        var numArray1 = new double[length1, Diagonal.Rows()];
        for (var index1 = 0; index1 < length1; ++index1)
            for (var index2 = 0; index2 < num1; ++index2)
                if (Math.Abs(Diagonal[index2]) > threshold)
                    numArray1[index1, index2] = RightSingularVectors[index1, index2] / Diagonal[index2];
        var length2 = LeftSingularVectors.Rows();
        var num2 = LeftSingularVectors.Columns();
        var numArray2 = new double[length1, length2];
        for (var index3 = 0; index3 < length1; ++index3)
            for (var index4 = 0; index4 < length2; ++index4)
            {
                var num3 = 0.0;
                for (var index5 = 0; index5 < num2; ++index5)
                    num3 += numArray1[index3, index5] * LeftSingularVectors[index4, index5];
                numArray2[index3, index4] = num3;
            }

        return numArray2;
    }

    public double[,] Reverse()
    {
        return LeftSingularVectors.Dot(DiagonalMatrix).DotWithTransposed(RightSingularVectors);
    }

    public double[,] GetInformationMatrix()
    {
        var threshold = Threshold;
        var length = RightSingularVectors.Rows();
        var num1 = RightSingularVectors.Columns();
        var numArray = new double[length, Diagonal.Rows()];
        for (var index1 = 0; index1 < length; ++index1)
            for (var index2 = 0; index2 < num1; ++index2)
                if (Math.Abs(Diagonal[index2]) > threshold)
                    numArray[index1, index2] = RightSingularVectors[index1, index2] / Diagonal[index2];
        var informationMatrix = new double[length, length];
        for (var index3 = 0; index3 < length; ++index3)
            for (var index4 = 0; index4 < length; ++index4)
            {
                var num2 = 0.0;
                for (var index5 = 0; index5 < length; ++index5)
                    num2 += numArray[index3, index5] * RightSingularVectors[index4, index5];
                informationMatrix[index3, index4] = num2;
            }

        return informationMatrix;
    }
}