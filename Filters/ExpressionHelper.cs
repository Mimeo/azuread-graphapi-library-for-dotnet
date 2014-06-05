// Copyright Â© Microsoft Open Technologies, Inc.
//
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
// ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A
// PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache License, Version 2.0 for the specific language
// governing permissions and limitations under the License.

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Reflection;
    using Newtonsoft.Json;

    /// <summary>
    /// Helper methods for generating query filter expressions.
    /// </summary>
    public class ExpressionHelper
    {
        /// <summary>
        /// MethodInfo for the compare method on string.
        /// </summary>
        public static readonly MethodInfo CompareMethodInfo = typeof(string).GetMethod(
                "Compare", new Type[] { typeof(string), typeof(string) });

        /// <summary>
        /// Method definition of the StartsWith method.
        /// </summary>
        public static readonly MethodInfo StartsWithMethodInfo = typeof(string).GetMethod(
            "StartsWith", new Type[] { typeof(string) });

        /// <summary>
        /// Validates whether the binary expression is valid for OData query filters.
        /// </summary>
        /// <param name="binaryExpression">Binary expression</param>
        public static void ValidateBinaryExpression(BinaryExpression binaryExpression)
        {
            Utils.ThrowIfNull(binaryExpression, "binaryExpression");
        }

        /// <summary>
        /// Validates whether the binary expression is valid for OData query filters.
        /// </summary>
        /// <param name="binaryExpression">Binary expression</param>
        public static void ValidateLeafExpression(BinaryExpression binaryExpression)
        {
            Utils.ThrowIfNull(binaryExpression, "binaryExpression");

            ExpressionHelper.ValidateLeafNode(binaryExpression.Left);
            ExpressionHelper.ValidateLeafNode(binaryExpression.Right);
        }

        /// <summary>
        /// Validates whether the binary expression is valid AND / OR expression.
        /// </summary>
        /// <param name="binaryExpression">Binary conjuctive expression.</param>
        public static void ValidateConjunctiveExpression(BinaryExpression binaryExpression)
        {
            Utils.ThrowIfNull(binaryExpression, "binaryExpression");

            ExpressionHelper.ValidateConjuctiveNode(binaryExpression.Left);
            ExpressionHelper.ValidateConjuctiveNode(binaryExpression.Right);
        }

        /// <summary>
        /// Validate whether the leaf node is valid.
        /// </summary>
        /// <param name="expression">Expression node.</param>
        public static void ValidateLeafNode(Expression expression)
        {
            Utils.ThrowIfNull(expression, "expression");

            if (expression.NodeType != ExpressionType.MemberAccess &&
                expression.NodeType != ExpressionType.Constant &&
                expression.NodeType != ExpressionType.Call)
            {
                throw new ArgumentException("The expression has an invalid leaf node.");
            }
        }

        /// <summary>
        /// Validate whether the node is a conjuctive node or not.
        /// </summary>
        /// <param name="expression">Expression node.</param>
        /// <remarks>Validates that the node is not a constant expression, but another sub expression.</remarks>
        public static void ValidateConjuctiveNode(Expression expression)
        {
            Utils.ThrowIfNull(expression, "expression");

            if (expression is ConstantExpression)
            {
                throw new ArgumentException("The expression has an invalid conjuctive node.");
            }
        }

        /// <summary>
        /// Creates an equals expression, based on the property name and value.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Equals expression node.</returns>
        public static BinaryExpression CreateEqualsExpression(
            Type entityType, GraphProperty propertyName, object propertyValue)
        {
            return ExpressionHelper.CreateConditionalExpression(
                entityType, propertyName, propertyValue, ExpressionType.Equal);
        }

        /// <summary>
        /// Creates a less than or equals expression, based on the property name and value.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Equals expression node.</returns>
        public static BinaryExpression CreateLessThanEqualsExpression(
            Type entityType, GraphProperty propertyName, object propertyValue)
        {
            return ExpressionHelper.CreateConditionalExpression(
                entityType, propertyName, propertyValue, ExpressionType.LessThanOrEqual);
        }

        /// <summary>
        /// Creates a greater than or equals expression, based on the property name and value.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Equals expression node.</returns>
        public static BinaryExpression CreateGreaterThanEqualsExpression(
            Type entityType, GraphProperty propertyName, object propertyValue)
        {
            return ExpressionHelper.CreateConditionalExpression(
                entityType, propertyName, propertyValue, ExpressionType.GreaterThanOrEqual);
        }

        /// <summary>
        /// Creates a less than or equals expression, based on the property name and value.
        /// </summary>
        /// <param name="entityTpe">Entity type.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Equals expression node.</returns>
        /// <remarks>
        /// Conditional expressions supported are Equals, GreaterThanEquals and LessThanEquals
        /// Equals binary expression is supported on all types.
        /// GreaterThanEquals and LessThanEquals are supported in int, long etc.. but not on
        /// strings. To generate these expressions on strings, we need to use the Compare function.
        /// </remarks>
        public static BinaryExpression CreateConditionalExpression(
            Type entityType, GraphProperty propertyName, object propertyValue, ExpressionType expressionType)
        {
            Utils.ThrowIfNull(entityType, "entityType");
            Utils.ThrowIfNullOrEmpty(propertyName, "propertyName");
            Utils.ThrowIfNullOrEmpty(propertyValue, "propertyValue");

            if (expressionType != ExpressionType.Equal &&
                expressionType != ExpressionType.GreaterThanOrEqual &&
                expressionType != ExpressionType.LessThanOrEqual)
            {
                throw new ArgumentException("Unsupported expression type.", "expressionType");
            }

            PropertyInfo propertyInfo;
            MemberExpression memberExpression = ExpressionHelper.GetMemberExpression(
                entityType, propertyName, out propertyInfo);

            Type propertyType = propertyInfo.PropertyType;

            string expectedPropertyTypeFullName = propertyType.FullName;

            // Nullable types have to converted before an Expression can be generated.
            bool isConversionRequired = false;
            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                expectedPropertyTypeFullName = propertyType.GetGenericArguments()[0].FullName;
                isConversionRequired = true;
            }

            string propertyValueTypeFullName = propertyValue.GetType().FullName;

            if (!String.Equals(expectedPropertyTypeFullName, propertyValueTypeFullName))
            {
                throw new ArgumentException("Property types do not match.");
            }

            // ge and le are currently supported only on int and strings.
            // TODO: Enable it for DateTime
            if (expressionType == ExpressionType.GreaterThanOrEqual ||
                expressionType == ExpressionType.LessThanOrEqual)
            {
                if (!string.Equals(typeof(string).FullName, expectedPropertyTypeFullName) &&
                    !string.Equals(typeof(int).FullName, expectedPropertyTypeFullName))
                {
                    throw new ArgumentException("Comparison is supported only on string and int values.");
                }
            }

            // Get the method info for Compare and create the expression such that
            // string.compare(propertyName, 'value') <= 0
            Expression methodCallExpression = null;

            if (expectedPropertyTypeFullName.Equals(typeof(string).FullName))
            {
                methodCallExpression = Expression.Call(
                    ExpressionHelper.CompareMethodInfo,
                    memberExpression,
                    Expression.Constant(propertyValue));
            }

            switch (expressionType)
            {
                case ExpressionType.Equal:
                    if (isConversionRequired)
                    {
                        return Expression.Equal(memberExpression, Expression.Constant(propertyValue, propertyType));
                    }

                    return Expression.Equal(memberExpression, Expression.Constant(propertyValue));
                case ExpressionType.LessThanOrEqual:
                    return Expression.LessThanOrEqual(methodCallExpression, Expression.Constant(0));
                case ExpressionType.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(methodCallExpression, Expression.Constant(0));
                default:
                    throw new ArgumentException("Unsupported expression type.");
            }
        }

        /// <summary>
        /// Get the member expression for the given property name.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyInfo">Set to the property info.</param>
        /// <returns>Member expression.</returns>
        public static MemberExpression GetMemberExpression(
            Type entityType, GraphProperty propertyName, out PropertyInfo propertyInfo)
        {
            Utils.ThrowIfNull(entityType, "entityType");

            MemberInfo[] memberInfo = entityType.GetMember(propertyName.ToString());
            if (memberInfo == null || memberInfo.Length != 1)
            {
                throw new ArgumentException("Unable to resolve the property in the specified type.");
            }

            propertyInfo = memberInfo[0] as PropertyInfo;

            // Validate that the JsonProperty attribute can be found on this property
            object[] customAttributes = propertyInfo.GetCustomAttributes(
                typeof(JsonPropertyAttribute), true);

            if (customAttributes == null || customAttributes.Length != 1)
            {
                throw new ArgumentException(
                    "Invalid property used in the filter. JsonProperty attribute was not found.");
            }

            // Create a member expression on a dummy object to create a valid expression.
            // BUGBUG: We shouldn't be creating a dummy instance. We should instead be generating the list from
            // a where clause. Like: 
            // var users = FROM graphConnection.Users SELECT users WHERE User.DisplayName == 'blah'
            MemberExpression memberExpression = Expression.MakeMemberAccess(
                Expression.Constant(Activator.CreateInstance(entityType)), memberInfo[0]);

            return memberExpression;
        }

        /// <summary>
        /// Creates a simple starts with expression, based on the property name and value.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Equals expression node.</returns>
        public static MethodCallExpression CreateStartsWithExpression(
            Type entityType, GraphProperty propertyName, string propertyValue)
        {
            Utils.ThrowIfNull(entityType, "entityType");
            Utils.ThrowIfNullOrEmpty(propertyName, "propertyName");
            Utils.ThrowIfNullOrEmpty(propertyValue, "propertyValue");

            PropertyInfo propertyInfo;
            MemberExpression memberExpression = ExpressionHelper.GetMemberExpression(
                entityType, propertyName, out propertyInfo);

            Type propertyType = propertyInfo.PropertyType;

            string expectedPropertyTypeFullName = propertyType.FullName;
            string propertyValueTypeFullName = propertyValue.GetType().FullName;

            if (!String.Equals(expectedPropertyTypeFullName, propertyValueTypeFullName))
            {
                throw new ArgumentException("Property types do not match.");
            }

            return Expression.Call(
                memberExpression, "StartsWith", new Type[]{ } , Expression.Constant(propertyValue));
        }

        /// <summary>
        /// Creates a simple any expression, based on the property name and value.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Equals expression node.</returns>
        public static MethodCallExpression CreateAnyExpression(
            Type entityType, GraphProperty propertyName, object propertyValue)
        {
            Utils.ThrowIfNull(entityType, "entityType");
            Utils.ThrowIfNullOrEmpty(propertyName, "propertyName");

            // TODO: Generate an empty Any() filter when the propertyValue is null.
            Utils.ThrowIfNullOrEmpty(propertyValue, "propertyValue");

            PropertyInfo propertyInfo;
            MemberExpression memberExpression = ExpressionHelper.GetMemberExpression(
                entityType, propertyName, out propertyInfo);

            Type propertyType = propertyInfo.PropertyType;
            Type[] genericArguments = propertyType.GetGenericArguments();

            if (!propertyType.IsGenericType ||
                genericArguments == null ||
                genericArguments.Length != 1 ||
                !propertyType.Name.StartsWith(typeof(ChangeTrackingCollection<>).Name))
            {
                throw new ArgumentException("Any expression is not supported on this type.");
            }

            string expectedPropertyTypeFullName = propertyType.GetGenericArguments()[0].FullName;
            string propertyValueTypeFullName = propertyValue.GetType().FullName;

            if (!String.Equals(expectedPropertyTypeFullName, propertyValueTypeFullName))
            {
                throw new ArgumentException("Property types do not match.");
            }

            return Expression.Call(
                memberExpression, "Any", new Type[] { }, Expression.Constant(propertyValue));
        }

        /// <summary>
        /// Create And expressions from the following binary expressions.
        /// </summary>
        /// <param name="left">First expressions.</param>
        /// <param name="right">Second expression.</param>
        /// <returns>Joined binary expression.</returns>
        public static BinaryExpression JoinExpressions(
            Expression left, Expression right, ExpressionType expressionType)
        {
            Utils.ThrowIfNullOrEmpty(left, "left");
            Utils.ThrowIfNullOrEmpty(right, "right");

            switch (expressionType)
            {
                case ExpressionType.And:
                    return Expression.And(left, right);
                case ExpressionType.Or:
                    return Expression.Or(left, right);
                default:
                    throw new ArgumentException("Invalid expressionType");
            }
        }

        /// <summary>
        /// Get the formatted filter value.
        /// </summary>
        /// <param name="filterValue">Raw filter value.</param>
        /// <returns>Formatted filter value.</returns>
        public static string GetFormattedValue(object filterValue)
        {
            if (filterValue is bool)
            {
                return filterValue.ToString().ToLower();
            }

            if (filterValue is Guid)
            {
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "Guid\'{0}\'",
                    filterValue);
            }

            byte[] filterValueAsBytes = filterValue as byte[];
            if (filterValueAsBytes != null)
            {
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "X\'{0}\'",
                    Utils.BinToHexEncode(filterValueAsBytes));
            }

            if (filterValue is DateTime)
            {
                DateTime dateTime = (DateTime)filterValue;
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "DateTime\'{0:s}Z\'",
                    dateTime.ToUniversalTime());
            }

            return "\'" + filterValue + "\'";
        }

        /// <summary>
        /// Get OData operator for the expression type.
        /// </summary>
        /// <param name="expressionType">Expression type.</param>
        /// <returns>OData operator.</returns>
        /// <exception cref="ArgumentException">Unsupported expression type.</exception>
        public static string GetODataOperator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Equal:
                    return "eq";
                case ExpressionType.GreaterThanOrEqual:
                    return "ge";
                case ExpressionType.LessThanOrEqual:
                    return "le";
                default:
                    throw new ArgumentException("Unsupported expression type");
            }
        }

        /// <summary>
        /// Get the property name from the constant expression.
        /// </summary>
        /// <param name="binaryExpression">Binary expression.</param>
        /// <returns>Property name.</returns>
        /// <exception cref="ArgumentNullException">Null input expression.</exception>
        /// <exception cref="ArgumentException">Not a constant expression.</exception>
        /// <remarks>
        /// Following expressions are supported.
        /// displayName eq 'blah'
        /// Compare(displayName, 'blah') ge 0
        /// </remarks>
        public static string GetPropertyName(BinaryExpression binaryExpression)
        {
            Utils.ThrowIfNull(binaryExpression, "binaryExpression");

            MemberExpression memberExpression = binaryExpression.Left as MemberExpression;

            // Check the RHS for a member expression.
            if (memberExpression == null)
            {
                memberExpression = binaryExpression.Right as MemberExpression;
            }

            if (memberExpression != null)
            {
                return ExpressionHelper.GetPropertyName(memberExpression);
            }

            MethodCallExpression methodCallExpression = binaryExpression.Left as MethodCallExpression;

            if (methodCallExpression == null)
            {
                methodCallExpression = binaryExpression.Right as MethodCallExpression;
            }

            if (methodCallExpression != null)
            {
                // Make sure that we are looking at the 2 argument compare method on a string.
                if (methodCallExpression.Method != ExpressionHelper.CompareMethodInfo)
                {
                    throw new ArgumentException("Unexpected method call in the binary expression.");
                }

                memberExpression = methodCallExpression.Arguments[0] as MemberExpression;

                if (memberExpression == null)
                {
                    memberExpression = methodCallExpression.Arguments[1] as MemberExpression;
                }

                if (memberExpression != null)
                {
                    return ExpressionHelper.GetPropertyName(memberExpression);
                }
            }

            throw new ArgumentException("Unable to extract the propertyName from the expression.");
        }

        /// <summary>
        /// Gets the property name from a member expresion.
        /// </summary>
        /// <param name="memberExpression">Member expression.</param>
        /// <returns>Property name.</returns>
        public static string GetPropertyName(MemberExpression memberExpression)
        {
            Utils.ThrowIfNull(memberExpression, "memberExpression");

            Utils.ThrowArgumentExceptionIfNullOrEmpty(memberExpression.Member, "Invalid MemberInfo");

            object[] customProperties = memberExpression.Member.GetCustomAttributes(
                typeof (JsonPropertyAttribute), true);

            if (customProperties == null || customProperties.Length != 1)
            {
                throw new ArgumentException("JsonProperty attribute was not found.");
            }

            JsonPropertyAttribute jsonPropertyAttribute = customProperties[0] as JsonPropertyAttribute;

            if (jsonPropertyAttribute == null)
            {
                throw new ArgumentException("JsonProperty attribute was not found.");
            }

            return jsonPropertyAttribute.PropertyName;
        }

        /// <summary>
        /// Get the property value from the binary expression.
        /// </summary>
        /// <param name="binaryExpression">Constant expression.</param>
        /// <returns>Property name.</returns>
        /// <exception cref="ArgumentNullException">Null input expression.</exception>
        /// <exception cref="ArgumentException">Not a constant expression.</exception>
        public static string GetPropertyValue(BinaryExpression binaryExpression)
        {
            Utils.ThrowIfNull(binaryExpression, "binaryExpression");

            // Check if this is a compare method.
            // Precedence for Compare method is a MUST as the the constant expression is found in both the cases.
            MethodCallExpression methodCallExpression = binaryExpression.Left as MethodCallExpression;

            if (methodCallExpression == null)
            {
                methodCallExpression = binaryExpression.Right as MethodCallExpression;
            }

            ConstantExpression constantExpression;
            if (methodCallExpression != null)
            {
                // Make sure that we are looking at the 2 argument compare method on a string.
                if (methodCallExpression.Method != ExpressionHelper.CompareMethodInfo)
                {
                    throw new ArgumentException("Unexpected method call in the binary expression.");
                }

                constantExpression = methodCallExpression.Arguments[1] as ConstantExpression;

                if (constantExpression == null)
                {
                    constantExpression = methodCallExpression.Arguments[0] as ConstantExpression;
                }

                if (constantExpression != null)
                {
                    return ExpressionHelper.GetPropertyValue(constantExpression);
                }
            }

            constantExpression = binaryExpression.Right as ConstantExpression;

            if (constantExpression == null)
            {
                constantExpression = binaryExpression.Left as ConstantExpression;
            }


            if (constantExpression != null)
            {
                return ExpressionHelper.GetPropertyValue(constantExpression);
            }

            throw new ArgumentException("Unable to extract the constant property value from expression.");            
        }

        /// <summary>
        /// Get the property value from the binary expression.
        /// </summary>
        /// <param name="constantExpression">Constant expression.</param>
        /// <returns>Property name.</returns>
        /// <exception cref="ArgumentNullException">Null input expression.</exception>
        /// <exception cref="ArgumentException">Not a constant expression.</exception>
        public static string GetPropertyValue(ConstantExpression constantExpression)
        {
            Utils.ThrowIfNull(constantExpression, "constantExpression");

            return ExpressionHelper.GetFormattedValue(constantExpression.Value);
        }

        /// <summary>
        /// Get OData conjuctive operator for the expression type.
        /// </summary>
        /// <param name="expressionType">Expression type.</param>
        /// <returns>OData conjuction operator.</returns>
        /// <exception cref="ArgumentException">Unsupported expression type.</exception>
        public static string GetODataConjuctiveOperator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.And:
                    return "and";
                case ExpressionType.Or:
                    return "or";
                default:
                    throw new ArgumentException("Unsupported expression type");
            }
        }
    }
}
