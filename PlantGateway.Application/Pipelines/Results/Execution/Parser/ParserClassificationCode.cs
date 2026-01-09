using PlantGateway.Core.Config.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    public enum ParserClassificationCode
    {
        [ExecutionResultClassificationAttribute(
            trueHeader: "Tag exists",
            falseHeader: "Tag missing",
            description: "Checks if the AvevaTag attribute is present.",
            category: "Validation",
            severity: "Error"
        )]
        CheckExists,

        [ExecutionResultClassificationAttribute(
            trueHeader: "Not empty",
            falseHeader: "Empty value",
            description: "Checks if the AvevaTag attribute has a non-empty value.",
            category: "Validation",
            severity: "Warning"
        )]
        CheckNotEmpty,

        [ExecutionResultClassificationAttribute(
            trueHeader: "Correct separators",
            falseHeader: "Invalid separators",
            description: "Checks if tag separators ('_','-','.') are used incorrectly.",
            category: "Format",
            severity: "Info"
        )]
        CheckSeparator,

        [ExecutionResultClassificationAttribute(
            trueHeader: "No duplicates",
            falseHeader: "Duplicate values detected",
            description: "Checks if duplicate tag values occur.",
            category: "Uniqueness",
            severity: "Warning"
        )]
        CheckDuplicate,

        [ExecutionResultClassificationAttribute(
            trueHeader: "Redirected to Limbo",
            falseHeader: "Limbo free",
            description: "Checks if AvevaTag provides sufficient data for correct import.",
            category: "Validation",
            severity: "Error"
        )]
        GoToLimbo,
    }
}
