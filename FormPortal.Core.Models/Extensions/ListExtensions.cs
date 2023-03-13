﻿using FormPortal.Core.Constants;
using FormPortal.Core.Interfaces;
using FormPortal.Core.Models;
using FormPortal.Core.Models.FormElements;

namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static void SetSortOrder<T>(this List<T> list) where T : IHasSortableElement
        {
            int i = 1;
            foreach (var item in list)
            {
                item.SortOrder = i++;
            }
        }

        public static bool ValidateRules(this List<Rule> rules)
        {
            if (!rules.Any())
            {
                return true;
            }


            List<(LogicalOperator logicOperator, bool result)> checkedRules = new();

            foreach (var rule in rules)
            {
                var element = rule.Element;
                if (element is FormNumberElement numberElement)
                {
                    bool result = rule.ComparisonOperator switch
                    {
                        ComparisonOperator.Equals => numberElement.Value == rule.ValueNumber,
                        ComparisonOperator.NotEquals => numberElement.Value != rule.ValueNumber,
                        ComparisonOperator.GreaterThan => numberElement.Value > rule.ValueNumber,
                        ComparisonOperator.LessThan => numberElement.Value < rule.ValueNumber,
                        ComparisonOperator.GreaterThanOrEqual => numberElement.Value >= rule.ValueNumber,
                        ComparisonOperator.LessThanOrEqual => numberElement.Value <= rule.ValueNumber,
                        _ => false,
                    };

                    checkedRules.Add((rule.LogicalOperator, result));
                }
                else if (element is FormElementWithOptions optionsElement)
                {
                    bool result = rule.ComparisonOperator switch
                    {
                        ComparisonOperator.Equals => optionsElement.Value == rule.ValueString,
                        ComparisonOperator.NotEquals => optionsElement.Value != rule.ValueString,
                        _ => false,
                    };

                    checkedRules.Add((rule.LogicalOperator, result));
                }
                else if (element is FormDateElement dateElement)
                {
                    bool result = rule.ComparisonOperator switch
                    {
                        ComparisonOperator.Equals => dateElement.Value.Date == rule.ValueDate.Date,
                        ComparisonOperator.NotEquals => dateElement.Value.Date != rule.ValueDate.Date,
                        ComparisonOperator.GreaterThan => dateElement.Value.Date > rule.ValueDate.Date,
                        ComparisonOperator.LessThan => dateElement.Value.Date < rule.ValueDate.Date,
                        ComparisonOperator.GreaterThanOrEqual => dateElement.Value.Date >= rule.ValueDate.Date,
                        ComparisonOperator.LessThanOrEqual => dateElement.Value.Date <= rule.ValueDate.Date,
                        _ => false,
                    };

                    checkedRules.Add((rule.LogicalOperator, result));
                }
                else if (element is FormCheckboxElement checkboxElement)
                {
                    bool result = rule.ComparisonOperator switch
                    {
                        ComparisonOperator.Equals => checkboxElement.Value == rule.ValueBoolean,
                        ComparisonOperator.NotEquals => checkboxElement.Value != rule.ValueBoolean,
                        _ => false,
                    };

                    checkedRules.Add((rule.LogicalOperator, result));
                }
            }

            // When someone has manipulated the values within the database then me might not have any results at all.
            if (!checkedRules.Any())
            {
                return false;
            }

            bool returnResult = checkedRules.First().result;

            for (int i = 1; i < checkedRules.Count; i++)
            {
                (LogicalOperator logicOperator, bool result) tmp = checkedRules[i];

                returnResult = tmp.logicOperator switch
                {
                    LogicalOperator.And => returnResult && tmp.result,
                    LogicalOperator.Or => returnResult || tmp.result,
                    _ => false,
                };
            }


            return returnResult;
        }
    }
}
