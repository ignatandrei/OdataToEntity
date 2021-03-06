﻿using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OdataToEntity.Parsers
{
    public static class OeExpressionHelper
    {
        private static readonly Type[] ArithmethicTypes = new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(Decimal) };

        public static ConstantExpression ConstantChangeType(ConstantExpression constantExpression, Type targetType)
        {
            if (constantExpression.Value == null)
                return constantExpression;
            if (constantExpression.Type == targetType)
                return constantExpression;

            Object value;
            if (constantExpression.Type == typeof(DateTimeOffset))
            {
                if (targetType == typeof(DateTimeOffset?))
                    return constantExpression;
                if (targetType == typeof(DateTime))
                    return Expression.Constant(((DateTimeOffset)constantExpression.Value).UtcDateTime);
            }
            else if (constantExpression.Type == typeof(Date))
            {
                if (targetType == typeof(Date?))
                    return constantExpression;
                if (targetType == typeof(DateTime))
                    return Expression.Constant((DateTime)(Date)constantExpression.Value);
            }
            else if (constantExpression.Type == typeof(ODataEnumValue))
            {
                var enumValue = (ODataEnumValue)constantExpression.Value;
                value = Enum.Parse(targetType, enumValue.Value);
                return Expression.Constant(value);
            }

            value = Convert.ChangeType(constantExpression.Value, targetType, CultureInfo.InvariantCulture);
            return Expression.Constant(value);
        }
        public static NewExpression CreateTupleExpression(IReadOnlyList<Expression> expressions)
        {
            if (expressions.Count < 8 || (expressions.Count == 8 && IsTupleType(expressions[7].Type)))
            {
                Type[] typeArguments = new Type[expressions.Count];
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    Type type = expressions[i].Type;
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>))
                        typeArguments[i] = typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments());
                    else
                        typeArguments[i] = type;
                }
                Type tupleType = GetTupleType(typeArguments);
                ConstructorInfo ctorInfo = tupleType.GetConstructor(typeArguments);
                return Expression.New(ctorInfo, expressions, tupleType.GetProperties());
            }

            NewExpression restNew = null;
            int count = expressions.Count;
            while (count > 0)
            {
                int len = count % 7;
                if (len == 0)
                    len = 7;

                Expression[] restExpressions;
                if (restNew == null)
                    restExpressions = new Expression[len];
                else
                {
                    restExpressions = new Expression[len + 1];
                    restExpressions[len] = restNew;
                }
                for (; len > 0; len--, count--)
                    restExpressions[len - 1] = expressions[count - 1];
                restNew = CreateTupleExpression(restExpressions);
            }
            return restNew;
        }
        private static Type GetArithmethicPrecedenceType(Type leftType, Type rightType)
        {
            if (leftType == rightType)
                return leftType;

            leftType = GetUnsignedToSignedType(leftType);
            rightType = GetUnsignedToSignedType(rightType);

            int leftIndex = Array.IndexOf(ArithmethicTypes, leftType);
            if (leftIndex == -1)
                throw new InvalidOperationException("cannot convert not numeric type");

            int rightIndex = Array.IndexOf(ArithmethicTypes, rightType);
            if (rightIndex == -1)
                throw new InvalidOperationException("cannot convert not numeric type");

            return ArithmethicTypes[Math.Max(leftIndex, rightIndex)];
        }
        public static Type GetCollectionItemType(Type collectionType)
        {
            if (collectionType.IsPrimitive)
                return null;

            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return collectionType.GetGenericArguments()[0];

            foreach (Type iface in collectionType.GetInterfaces())
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            return null;
        }
        public static IReadOnlyList<MemberExpression> GetPropertyExpressions(Expression instance)
        {
            PropertyInfo[] properties = instance.Type.GetProperties();
            var expressions = new List<MemberExpression>(properties.Length);
            for (int i = 0; i < properties.Length; i++)
            {
                MemberExpression propertyExpression = Expression.Property(instance, properties[i]);
                if (i == 7 && IsTupleType(properties[7].PropertyType))
                    expressions.AddRange(GetPropertyExpressions(propertyExpression));
                else
                    expressions.Add(propertyExpression);
            }
            return expressions;
        }
        private static Type GetTupleType(Type[] typeArguments)
        {
            Type tupleType;
            switch (typeArguments.Length)
            {
                case 1:
                    tupleType = typeof(Tuple<>);
                    break;
                case 2:
                    tupleType = typeof(Tuple<,>);
                    break;
                case 3:
                    tupleType = typeof(Tuple<,,>);
                    break;
                case 4:
                    tupleType = typeof(Tuple<,,,>);
                    break;
                case 5:
                    tupleType = typeof(Tuple<,,,,>);
                    break;
                case 6:
                    tupleType = typeof(Tuple<,,,,,>);
                    break;
                case 7:
                    tupleType = typeof(Tuple<,,,,,,>);
                    break;
                case 8:
                    tupleType = typeof(Tuple<,,,,,,,>);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeArguments), "Tuple out of range");
            }

            return tupleType.MakeGenericType(typeArguments);
        }
        public static Type GetTypeConversion(Type leftType, Type rightType)
        {
            Type leftUnderlyingType = Nullable.GetUnderlyingType(leftType);
            Type rightUnderlyingType = Nullable.GetUnderlyingType(rightType);

            if (leftUnderlyingType == null && rightUnderlyingType == null)
                return GetArithmethicPrecedenceType(leftType, rightType);

            Type precedenceType;
            if (leftUnderlyingType == null)
            {
                if (leftType == rightUnderlyingType)
                    return typeof(Nullable<>).MakeGenericType(leftType);

                precedenceType = GetArithmethicPrecedenceType(leftType, rightUnderlyingType);
                return typeof(Nullable<>).MakeGenericType(precedenceType);
            }

            if (rightUnderlyingType == null)
            {
                if (rightType == leftUnderlyingType)
                    return typeof(Nullable<>).MakeGenericType(rightType);

                precedenceType = GetArithmethicPrecedenceType(rightType, leftUnderlyingType);
                return typeof(Nullable<>).MakeGenericType(precedenceType);
            }

            precedenceType = GetArithmethicPrecedenceType(leftUnderlyingType, rightUnderlyingType);
            return typeof(Nullable<>).MakeGenericType(precedenceType);
        }
        private static Type GetUnsignedToSignedType(Type type)
        {
            if (type == typeof(byte))
                return typeof(short);
            if (type == typeof(ushort))
                return typeof(int);
            if (type == typeof(uint))
                return typeof(long);
            if (type == typeof(ulong))
                return typeof(Decimal);
            return type;
        }
        public static bool IsNull(Expression expression)
        {
            return expression is ConstantExpression constantExpression ? constantExpression.Value == null : false;
        }
        public static bool IsNullable(Expression expression)
        {
            Type type = expression.Type;
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static bool IsTupleType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            Type tupleType = type.GetGenericTypeDefinition();
            return
                tupleType == typeof(Tuple<>) ||
                tupleType == typeof(Tuple<,>) ||
                tupleType == typeof(Tuple<,,>) ||
                tupleType == typeof(Tuple<,,,>) ||
                tupleType == typeof(Tuple<,,,,>) ||
                tupleType == typeof(Tuple<,,,,,>) ||
                tupleType == typeof(Tuple<,,,,,,>) ||
                tupleType == typeof(Tuple<,,,,,,,>);
        }
        public static ExpressionType ToExpressionType(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case BinaryOperatorKind.Or:
                    return ExpressionType.OrElse;
                case BinaryOperatorKind.And:
                    return ExpressionType.AndAlso;
                case BinaryOperatorKind.Equal:
                    return ExpressionType.Equal;
                case BinaryOperatorKind.NotEqual:
                    return ExpressionType.NotEqual;
                case BinaryOperatorKind.GreaterThan:
                    return ExpressionType.GreaterThan;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return ExpressionType.GreaterThanOrEqual;
                case BinaryOperatorKind.LessThan:
                    return ExpressionType.LessThan;
                case BinaryOperatorKind.LessThanOrEqual:
                    return ExpressionType.LessThanOrEqual;
                case BinaryOperatorKind.Add:
                    return ExpressionType.Add;
                case BinaryOperatorKind.Subtract:
                    return ExpressionType.Subtract;
                case BinaryOperatorKind.Multiply:
                    return ExpressionType.Multiply;
                case BinaryOperatorKind.Divide:
                    return ExpressionType.Divide;
                case BinaryOperatorKind.Modulo:
                    return ExpressionType.Modulo;
                case BinaryOperatorKind.Has:
                    throw new NotImplementedException(nameof(BinaryOperatorKind.Has));
                default:
                    throw new ArgumentOutOfRangeException(nameof(operatorKind));
            }
        }
        public static bool TryGetConstantValue(Expression expression, out Object value)
        {
            value = null;

            MemberExpression propertyExpression = expression as MemberExpression;
            if (propertyExpression == null && expression is UnaryExpression convertExpression)
                propertyExpression = convertExpression.Operand as MemberExpression;

            if (propertyExpression == null)
                return false;

            if (propertyExpression.Expression is ConstantExpression constantExpression)
            {
                value = (propertyExpression.Member as PropertyInfo).GetValue(constantExpression.Value);
                return true;
            }

            return false;
        }
    }
}
