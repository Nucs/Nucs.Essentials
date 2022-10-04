using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Nucs.Exceptions;

namespace Nucs {
    public static class QuantMathExtensions {
        /// <param name="decimals">How many digits after decimal point to round, passing 2 with <paramref name="price"/> of 11.3244 will round to 11.32</param>
        /// <param name="floor">Will floor <paramref name="decimals"/> decimal point, otherwise will round it to nearest</param>
        /// <returns>The rounded price</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? RoundPrice(this double? price, int decimals = AbstractInfrastructure.RoundingDecimals, bool floor = true) {
            if (!price.HasValue)
                return price;
            return floor
                ? Math.Floor(price.Value * Math.Pow(10, decimals)) / Math.Pow(10, decimals)
                : Math.Round(price.Value, decimals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double FiniteOr(this double? value, double fallback) {
            return value.HasValue && double.IsFinite(value.Value) ? value.Value : fallback;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float FiniteOr(this float? value, float fallback) {
            return value.HasValue && float.IsFinite(value.Value) ? value.Value : fallback;
        }
    }

    public static class NullableQuantMaths {
        #region Regular

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel more risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsMoreRisky(double currentLevel, double newLevel, bool isLong) {
            return isLong ? newLevel < currentLevel : newLevel > currentLevel;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more risky than <paramref name="left"/>
        ///     for a stoploss level and return the riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>More riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MoreRisk(double left, double right, bool isLong) {
            return IsMoreRisky(left, right, isLong) ? right : left;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel less risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsLessRisky(double currentLevel, double newLevel, bool isLong) {
            return isLong ? newLevel > currentLevel : newLevel < currentLevel;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less risky than <paramref name="left"/>
        ///     for a stoploss level and return the less riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Less riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double LessRisk(double left, double right, bool isLong) {
            return IsLessRisky(left, right, isLong) ? right : left;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel more profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsMoreProfitable(double currentLevel, double newLevel, bool isLong) {
            return isLong ? newLevel > currentLevel : newLevel < currentLevel;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more profitable than <paramref name="left"/>
        ///     for a profit target level and return the more profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>More profittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MoreProfit(double left, double right, bool isLong) {
            return IsMoreProfitable(left, right, isLong) ? right : left;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel less profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsLessProfitable(double currentLevel, double newLevel, bool isLong) {
            return isLong ? newLevel < currentLevel : newLevel > currentLevel;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less profitable than <paramref name="left"/>
        ///     for a profit target level and return the less profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Less lrofittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double LessProfit(double left, double right, bool isLong) {
            return IsLessProfitable(left, right, isLong) ? right : left;
        }

        #endregion Regular

        #region Lhs Nullable

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel more risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsMoreRisky(double? currentLevel, double newLevel, bool isLong) {
            return currentLevel.HasValue ? isLong ? newLevel < currentLevel.Value : newLevel > currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more risky than <paramref name="left"/>
        ///     for a stoploss level and return the riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>More riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MoreRisk(double? left, double right, bool isLong) {
            return (IsMoreRisky(left, right, isLong) ?? true) ? right : left!.Value;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel less risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsLessRisky(double? currentLevel, double newLevel, bool isLong) {
            return currentLevel.HasValue ? isLong ? newLevel > currentLevel.Value : newLevel < currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less risky than <paramref name="left"/>
        ///     for a stoploss level and return the less riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Less riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double LessRisk(double? left, double right, bool isLong) {
            return IsLessRisky(left, right, isLong) ?? true ? right : left!.Value;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel more profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsMoreProfitable(double? currentLevel, double newLevel, bool isLong) {
            return currentLevel.HasValue ? isLong ? newLevel > currentLevel.Value : newLevel < currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more profitable than <paramref name="left"/>
        ///     for a profit target level and return the more profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>More profittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MoreProfit(double? left, double right, bool isLong) {
            return IsMoreProfitable(left, right, isLong) ?? true ? right : left!.Value;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel less profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsLessProfitable(double? currentLevel, double newLevel, bool isLong) {
            return currentLevel.HasValue ? isLong ? newLevel < currentLevel.Value : newLevel > currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less profitable than <paramref name="left"/>
        ///     for a profit target level and return the less profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Less lrofittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double LessProfit(double? left, double right, bool isLong) {
            return IsLessProfitable(left, right, isLong) ?? true ? right : left!.Value;
        }

        #endregion

        #region Rhs Nullable

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel more risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsMoreRisky(double currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue ? isLong ? newLevel.Value < currentLevel : newLevel.Value > currentLevel : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more risky than <paramref name="left"/>
        ///     for a stoploss level and return the riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>More riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MoreRisk(double left, double? right, bool isLong) {
            return IsMoreRisky(left, right, isLong) ?? false ? right!.Value : left;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel less risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsLessRisky(double currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue ? isLong ? newLevel.Value > currentLevel : newLevel.Value < currentLevel : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less risky than <paramref name="left"/>
        ///     for a stoploss level and return the less riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Less riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double LessRisk(double left, double? right, bool isLong) {
            return IsLessRisky(left, right, isLong) ?? false ? right!.Value : left;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel more profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsMoreProfitable(double currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue ? isLong ? newLevel.Value > currentLevel : newLevel.Value < currentLevel : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more profitable than <paramref name="left"/>
        ///     for a profit target level and return the more profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>More profittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MoreProfit(double left, double? right, bool isLong) {
            return IsMoreProfitable(left, right, isLong) ?? false ? right!.Value : left;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel less profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsLessProfitable(double currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue ? isLong ? newLevel.Value < currentLevel : newLevel.Value > currentLevel : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less profitable than <paramref name="left"/>
        ///     for a profit target level and return the less profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Less lrofittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double LessProfit(double left, double? right, bool isLong) {
            return IsLessProfitable(left, right, isLong) ?? false ? right!.Value : left;
        }

        #endregion Rhs Nullable

        #region Both Nullable

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel more risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsMoreRisky(double? currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue && currentLevel.HasValue ? isLong ? newLevel.Value < currentLevel.Value : newLevel.Value > currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more risky than <paramref name="left"/>
        ///     for a stoploss level and return the riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>More riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? MoreRisk(double? left, double? right, bool isLong) {
            return IsMoreRisky(left, right, isLong) ?? false ? right ?? left : left ?? right;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less risky than <paramref name="currentLevel"/>
        ///     for a stoploss level.
        /// </summary>
        /// <param name="currentLevel">Current stoploss level</param>
        /// <param name="newLevel">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Is newLevel less risky</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsLessRisky(double? currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue && currentLevel.HasValue ? isLong ? newLevel.Value > currentLevel.Value : newLevel.Value < currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less risky than <paramref name="left"/>
        ///     for a stoploss level and return the less riskier level.
        /// </summary>
        /// <param name="left">Current stoploss level</param>
        /// <param name="right">New stoploss level</param>
        /// <param name="isLong">Is it a long stoploss</param>
        /// <returns>Less riskier level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? LessRisk(double? left, double? right, bool isLong) {
            return IsLessRisky(left, right, isLong) ?? false ? right ?? left : left ?? right;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be more profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel more profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsMoreProfitable(double? currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue && currentLevel.HasValue ? isLong ? newLevel.Value > currentLevel.Value : newLevel.Value < currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be more profitable than <paramref name="left"/>
        ///     for a profit target level and return the more profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>More profittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? MoreProfit(double? left, double? right, bool isLong) {
            return IsMoreProfitable(left, right, isLong) ?? false ? right ?? left : left ?? right;
        }

        /// <summary>
        ///     Will check if the <paramref name="newLevel"/> will be less profitable than <paramref name="currentLevel"/>
        ///     for a profit target level.
        /// </summary>
        /// <param name="currentLevel">Current profit target level</param>
        /// <param name="newLevel">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Is newLevel less profitable</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool? IsLessProfitable(double? currentLevel, double? newLevel, bool isLong) {
            return newLevel.HasValue && currentLevel.HasValue ? isLong ? newLevel.Value < currentLevel.Value : newLevel.Value > currentLevel.Value : null;
        }

        /// <summary>
        ///     Will check if the <paramref name="right"/> will be less profitable than <paramref name="left"/>
        ///     for a profit target level and return the less profittable level..
        /// </summary>
        /// <param name="left">Current profit target level</param>
        /// <param name="right">New profit target level</param>
        /// <param name="isLong">Is it a long profit target</param>
        /// <returns>Less lrofittable level between <paramref name="left"/> and <paramref name="right"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? LessProfit(double? left, double? right, bool isLong) {
            return IsLessProfitable(left, right, isLong) ?? false ? right ?? left : left ?? right;
        }

        #endregion Rhs Nullable


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MinShareSize(double lhs, double rhs) {
            if (Math.Sign(lhs) != Math.Sign(rhs))
                throw new MathException($"The sign of lhs ({lhs}) and rhs {(rhs)} are mismatching polarity (sign)");

            return lhs > 0 ? Math.Min(lhs, rhs) : Math.Max(lhs, rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MinShareSize(double a, double b, double c) {
            return MinShareSize(MinShareSize(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double MinShareSize(double a, double b, double c, double d) {
            return MinShareSize(MinShareSize(MinShareSize(a, b), c), d);
        }

        /// <param name="decimals">How many digits after decimal point to round, passing 2 with <paramref name="price"/> of 11.3244 will round to 11.32</param>
        /// <param name="floor">Will floor <paramref name="decimals"/> decimal point, otherwise will round it to nearest</param>
        /// <returns>The rounded price</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double RoundPrice(double price, int decimals = AbstractInfrastructure.RoundingDecimals, bool floor = true) {
            return floor
                ? Math.Floor(price * Math.Pow(10, decimals)) / Math.Pow(10, decimals)
                : Math.Round(price, decimals);
        }

        /// <param name="decimals">How many digits after decimal point to round, passing 2 with <paramref name="price"/> of 11.3244 will round to 11.32</param>
        /// <param name="floor">Will floor <paramref name="decimals"/> decimal point, otherwise will round it to nearest</param>
        /// <returns>The rounded price</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? RoundPrice(double? price, int decimals = AbstractInfrastructure.RoundingDecimals, bool floor = true) {
            if (!price.HasValue)
                return price;
            return floor
                ? Math.Floor(price.Value * Math.Pow(10, decimals)) / Math.Pow(10, decimals)
                : Math.Round(price.Value, decimals);
        }

        /// <param name="flooringDecimals">Number of 10s to floor, passing 2 will round 234 to 200.</param>
        /// <param name="floor">Will floor decimal point, otherwise will round it to nearest</param>
        /// <returns>Rounded quantity</returns>
        public static long RoundQuantity(double quantity, int flooringDecimals = 2, bool floor = false) {
            var polarity = Math.Sign(quantity);
            quantity = Math.Abs(quantity);

            if (flooringDecimals == 0) {
                if (floor)
                    return polarity * (int) quantity;
                return polarity * (int) Math.Round(quantity);
            }

            if (floor) {
                if (quantity < (Math.Pow(10, flooringDecimals)))
                    return polarity * (int) Math.Floor(quantity);

                return polarity * (int) (Math.Floor(quantity) / ((int) Math.Pow(10, flooringDecimals)))
                                * ((int) Math.Pow(10, flooringDecimals));
            }

            if (quantity < (Math.Pow(10, flooringDecimals)))
                return polarity * (int) Math.Round(quantity, 0);

            return polarity * (int) (Math.Round(quantity, 0) / ((int) Math.Pow(10, flooringDecimals)))
                            * ((int) Math.Pow(10, flooringDecimals));
        }

        /// <param name="flooringDecimals">Number of 10s to floor, passing 2 will round 234 to 200.</param>
        /// <param name="floor">Will floor decimal point, otherwise will round it to nearest</param>
        /// <returns>Rounded quantity</returns>
        public static long RoundQuantity(long quantity, int flooringDecimals = 2, bool floor = false) {
            var polarity = Math.Sign(quantity);
            quantity = Math.Abs(quantity);

            if (flooringDecimals == 0) {
                if (floor)
                    return polarity * quantity;
                return polarity * quantity;
            }

            if (floor) {
                if (quantity < (Math.Pow(10, flooringDecimals)))
                    return polarity * quantity;

                return polarity * (quantity / ((long) Math.Pow(10, flooringDecimals)))
                                * ((long) Math.Pow(10, flooringDecimals));
            }

            if (quantity < (Math.Pow(10, flooringDecimals)))
                return polarity * quantity;

            return polarity * (quantity / ((long) Math.Pow(10, flooringDecimals)))
                            * ((long) Math.Pow(10, flooringDecimals));
        }


        /// <param name="flooringDecimals">Number of 10s to floor, passing 2 will round 234 to 200.</param>
        /// <param name="floor">Will floor decimal point, otherwise will round it to nearest</param>
        /// <returns>Rounded quantity</returns>
        public static int RoundQuantity(int quantity, int flooringDecimals = 2, bool floor = false) {
            var polarity = Math.Sign(quantity);
            quantity = Math.Abs(quantity);

            if (flooringDecimals == 0) {
                if (floor)
                    return polarity * quantity;
                return polarity * quantity;
            }

            if (floor) {
                if (quantity < (Math.Pow(10, flooringDecimals)))
                    return polarity * quantity;

                return polarity * (quantity / ((int) Math.Pow(10, flooringDecimals)))
                                * ((int) Math.Pow(10, flooringDecimals));
            }

            if (quantity < (Math.Pow(10, flooringDecimals)))
                return polarity * quantity;

            return polarity * (quantity / ((int) Math.Pow(10, flooringDecimals)))
                            * ((int) Math.Pow(10, flooringDecimals));
        }

        /// <param name="decimals">How many digits after decimal point to floor, passing 2 with <paramref name="price"/> of 11.3244 will round to 11.32</param>
        /// <returns>The rounded price</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double FloorPrice(double price) {
            Debug.Assert(AbstractInfrastructure.RoundingDecimals == 2);
            const int factor = 10 * 10; //AbstractInfrastructure.RoundingDecimals 
            return Math.Floor(price * factor) / factor;
        }

        /// <param name="decimals">How many digits after decimal point to floor, passing 2 with <paramref name="price"/> of 11.3244 will round to 11.32</param>
        /// <returns>The rounded price</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double FloorPolarPrice(double price) {
            Debug.Assert(AbstractInfrastructure.RoundingDecimals == 2);
            const int factor = 10 * 10; //AbstractInfrastructure.RoundingDecimals 
            return Math.Sign(price) * (Math.Floor(Math.Abs(price) * factor) / factor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double Min(double a, double b, double c) {
            return Math.Min(Math.Min(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float Min(float a, float b, float c) {
            return Math.Min(Math.Min(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Min(int a, int b, int c) {
            return Math.Min(Math.Min(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static long Min(long a, long b, long c) {
            return Math.Min(Math.Min(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double Max(double a, double b, double c) {
            return Math.Max(Math.Max(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float Max(float a, float b, float c) {
            return Math.Max(Math.Max(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Max(int a, int b, int c) {
            return Math.Max(Math.Max(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static long Max(long a, long b, long c) {
            return Math.Max(Math.Max(a, b), c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double Min(double a, double b, double c, double d) {
            return Math.Min(Math.Min(Math.Min(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float Min(float a, float b, float c, float d) {
            return Math.Min(Math.Min(Math.Min(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Min(int a, int b, int c, int d) {
            return Math.Min(Math.Min(Math.Min(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static long Min(long a, long b, long c, long d) {
            return Math.Min(Math.Min(Math.Min(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double Max(double a, double b, double c, double d) {
            return Math.Max(Math.Max(Math.Max(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float Max(float a, float b, float c, float d) {
            return Math.Max(Math.Max(Math.Max(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Max(int a, int b, int c, int d) {
            return Math.Max(Math.Max(Math.Max(a, b), c), d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static long Max(long a, long b, long c, long d) {
            return Math.Max(Math.Max(Math.Max(a, b), c), d);
        }

        /// <summary>
        ///     Creates a range of numbers starting <paramref name="from"/> and going up <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The number to start from, including</param>
        /// <param name="to">The number to end at, including</param>
        /// <param name="parts">How many items to return</param>
        /// <returns></returns>
        public static IEnumerable<double> Range(double @from, double to, double parts) {
            if (parts <= 0)
                yield break;

            parts -= 1;
            double v, step = (to - @from) / (parts);
            for (v = @from; v < to; v += step) {
                yield return v;
            }

            yield return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsMissingValue(this double lhs) {
            return Math.Abs(lhs - AbstractInfrastructure.MissingValue) < 0.00000001;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsMissingValue(this int lhs) {
            return lhs == AbstractInfrastructure.MissingValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool HasValue(this double lhs) {
            return Math.Abs(lhs - AbstractInfrastructure.MissingValue) > 0.00000001;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool HasValue(this int lhs) {
            return lhs != AbstractInfrastructure.MissingValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsMissingValueOrZero(this double lhs) {
            return lhs == 0d || Math.Abs(lhs - AbstractInfrastructure.MissingValue) < 0.00000001;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool IsMissingValueOrZero(this int lhs) {
            return lhs == AbstractInfrastructure.MissingValue || lhs == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool HasNonZeroOrMissingValue(this double lhs) {
            return lhs != 0d && Math.Abs(lhs - AbstractInfrastructure.MissingValue) > 0.00000001;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool HasNonZeroOrMissingValue(this int lhs) {
            return lhs != AbstractInfrastructure.MissingValue && lhs != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double Clip(double min, double max, double value) {
            if (value > max)
                value = max;
            else if (value < min)
                value = min;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Clip(int min, int max, int value) {
            if (value > max)
                value = max;
            else if (value < min)
                value = min;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float Clip(float min, float max, float value) {
            if (value > max)
                value = max;
            else if (value < min)
                value = min;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static byte Clip(byte min, byte max, byte value) {
            if (value > max)
                value = max;
            else if (value < min)
                value = min;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int? Abs(int? calculateSize) {
            if (!calculateSize.HasValue)
                return default;
            return Math.Abs(calculateSize.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static long? Abs(long? calculateSize) {
            if (!calculateSize.HasValue)
                return default;
            return Math.Abs(calculateSize.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double? Abs(double? calculateSize) {
            if (!calculateSize.HasValue)
                return default;
            return Math.Abs(calculateSize.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static short? Abs(short? calculateSize) {
            if (!calculateSize.HasValue)
                return default;
            return Math.Abs(calculateSize.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float? Abs(float? calculateSize) {
            if (!calculateSize.HasValue)
                return default;
            return Math.Abs(calculateSize.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static double FiniteOr(double value, double fallback) {
            return double.IsFinite(value) ? value : fallback;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float FiniteOr(float value, float fallback) {
            return float.IsFinite(value) ? value : fallback;
        }
    }
}