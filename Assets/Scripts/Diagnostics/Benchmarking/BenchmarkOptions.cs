using System;
using System.Globalization;

namespace Rustline.Diagnostics.Benchmarking
{
    public enum BenchmarkMode
    {
        PenumbraAb,
        ControlOff,
        ControlOn
    }

    [Serializable]
    public sealed class BenchmarkOptions
    {
        public const string ActivationFlag = "--rustline-benchmark";
        public const string DefaultQualityName = "Very Low";
        public const double DefaultWarmupSeconds = 15.0;
        public const double DefaultSettleSeconds = 2.0;
        public const double DefaultBlockSeconds = 15.0;
        public const int DefaultPairCount = 6;

        public bool requested;
        public BenchmarkMode mode = BenchmarkMode.PenumbraAb;
        public double warmupSeconds = DefaultWarmupSeconds;
        public double settleSeconds = DefaultSettleSeconds;
        public double blockSeconds = DefaultBlockSeconds;
        public int pairCount = DefaultPairCount;
        public bool autoQuit;
        public bool diagnostics;
        public string gitCommit = "unknown";
        public string gitDirtyState = "unknown";

        public static bool TryParse(string[] arguments, out BenchmarkOptions options, out string error)
        {
            options = new BenchmarkOptions();
            error = null;

            if (arguments == null)
            {
                return true;
            }

            for (int index = 0; index < arguments.Length; index++)
            {
                string argument = arguments[index];
                if (string.Equals(argument, ActivationFlag, StringComparison.OrdinalIgnoreCase))
                {
                    options.requested = true;
                    continue;
                }

                if (string.Equals(argument, "--benchmark-auto-quit", StringComparison.OrdinalIgnoreCase))
                {
                    options.autoQuit = true;
                    continue;
                }

                if (string.Equals(argument, "--benchmark-diagnostics", StringComparison.OrdinalIgnoreCase))
                {
                    options.diagnostics = true;
                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-mode", out string modeValue))
                {
                    if (!TryParseMode(modeValue, out options.mode))
                    {
                        error = $"Unknown benchmark mode '{modeValue}'. Expected penumbra-ab, control-off, or control-on.";
                        return false;
                    }

                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-warmup-seconds", out string warmupValue))
                {
                    if (!TryParseSeconds(warmupValue, allowZero: true, maximum: 600.0, out options.warmupSeconds))
                    {
                        error = "Benchmark warm-up must be a number from 0 through 600 seconds.";
                        return false;
                    }

                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-settle-seconds", out string settleValue))
                {
                    if (!TryParseSeconds(settleValue, allowZero: true, maximum: 60.0, out options.settleSeconds))
                    {
                        error = "Benchmark settling time must be a number from 0 through 60 seconds.";
                        return false;
                    }

                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-block-seconds", out string blockValue))
                {
                    if (!TryParseSeconds(blockValue, allowZero: false, maximum: 600.0, out options.blockSeconds))
                    {
                        error = "Benchmark block time must be a number greater than 0 and no greater than 600 seconds.";
                        return false;
                    }

                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-pairs", out string pairValue))
                {
                    if (!int.TryParse(pairValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out options.pairCount) ||
                        options.pairCount < 1 || options.pairCount > 100)
                    {
                        error = "Benchmark pair count must be an integer from 1 through 100.";
                        return false;
                    }

                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-git-commit", out string commitValue))
                {
                    options.gitCommit = string.IsNullOrWhiteSpace(commitValue) ? "unknown" : commitValue;
                    continue;
                }

                if (TryReadValue(arguments, ref index, "--benchmark-git-dirty", out string dirtyValue))
                {
                    options.gitDirtyState = string.IsNullOrWhiteSpace(dirtyValue) ? "unknown" : dirtyValue;
                }
            }

            return true;
        }

        public static string ToModeName(BenchmarkMode mode)
        {
            switch (mode)
            {
                case BenchmarkMode.ControlOff:
                    return "control-off";
                case BenchmarkMode.ControlOn:
                    return "control-on";
                default:
                    return "penumbra-ab";
            }
        }

        private static bool TryParseMode(string value, out BenchmarkMode mode)
        {
            if (string.Equals(value, "penumbra-ab", StringComparison.OrdinalIgnoreCase))
            {
                mode = BenchmarkMode.PenumbraAb;
                return true;
            }

            if (string.Equals(value, "control-off", StringComparison.OrdinalIgnoreCase))
            {
                mode = BenchmarkMode.ControlOff;
                return true;
            }

            if (string.Equals(value, "control-on", StringComparison.OrdinalIgnoreCase))
            {
                mode = BenchmarkMode.ControlOn;
                return true;
            }

            mode = BenchmarkMode.PenumbraAb;
            return false;
        }

        private static bool TryParseSeconds(
            string value,
            bool allowZero,
            double maximum,
            out double seconds)
        {
            bool parsed = double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out seconds);
            return parsed && !double.IsNaN(seconds) && !double.IsInfinity(seconds) &&
                   (allowZero ? seconds >= 0.0 : seconds > 0.0) && seconds <= maximum;
        }

        private static bool TryReadValue(
            string[] arguments,
            ref int index,
            string optionName,
            out string value)
        {
            string argument = arguments[index];
            string prefix = optionName + "=";
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = argument.Substring(prefix.Length);
                return true;
            }

            if (!string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase))
            {
                value = null;
                return false;
            }

            if (index + 1 >= arguments.Length)
            {
                value = string.Empty;
                return true;
            }

            index++;
            value = arguments[index];
            return true;
        }
    }

    public static class BenchmarkQuality
    {
        public static int FindLevelIndex(string[] availableNames, string requiredName)
        {
            if (availableNames == null || string.IsNullOrEmpty(requiredName))
            {
                return -1;
            }

            for (int index = 0; index < availableNames.Length; index++)
            {
                if (string.Equals(availableNames[index], requiredName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
