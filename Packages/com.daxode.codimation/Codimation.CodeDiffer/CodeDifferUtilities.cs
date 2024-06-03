using System.Collections.Generic;
using System.Text;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using UnityEngine;

public static class CodeDifferUtilities
{
    public static string CodeLerpUsingBudget(string sourceA, string sourceB, ref int budget)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(sourceA, sourceB);

        // var stringBuilder = new StringBuilder();
        // var sideStringBuilder = new StringBuilder();
        // var sideSideStringBuilder = new StringBuilder();
        
        var finalString = new StringBuilder();
        var deletedStack = new Stack<string>();
        foreach (var diffLine in diff.Lines)
        {
            switch (diffLine.Type)
            {
                case DiffPlex.DiffBuilder.Model.ChangeType.Deleted:
                    deletedStack.Push(diffLine.Text);
                    break;
                case DiffPlex.DiffBuilder.Model.ChangeType.Inserted:
                    if (!deletedStack.TryPop(out var deleted))
                    {
                        if (budget > 0)
                        {
                            var howMuchOfLineCanBeInserted = Mathf.Min(budget, diffLine.Text.Length);
                            finalString.AppendLine(diffLine.Text[..howMuchOfLineCanBeInserted]);
                            budget -= howMuchOfLineCanBeInserted;
                            break;
                        }
                        
                        // stringBuilder.Append("<color=green>");
                        // stringBuilder.Append(diffLine.Text);
                        // stringBuilder.AppendLine("</color>");
                        break;
                    }
                    
                    var modified = diffBuilder.BuildDiffModel(deleted, diffLine.Text, true, false, new LineChunker());

                    var modifiedDeletedStack = new Queue<string>();
                    var calledOnce = false;
                    foreach (var modifiedLine in modified.Lines)
                    {
                        if (!calledOnce)
                            calledOnce = true;
                        else if (modifiedLine.Type != DiffPlex.DiffBuilder.Model.ChangeType.Deleted)
                            finalString.Append(' ');
                        
                        switch (modifiedLine.Type)
                        {
                            case DiffPlex.DiffBuilder.Model.ChangeType.Deleted:
                                modifiedDeletedStack.Enqueue(modifiedLine.Text);
                                
                                // sideStringBuilder.Append("<color=red>");
                                // sideStringBuilder.Append(modifiedLine.Text);
                                // sideStringBuilder.AppendLine("</color>");
                                
                                break;
                            case DiffPlex.DiffBuilder.Model.ChangeType.Inserted:
                                // sideStringBuilder.Append("<color=green>");
                                // sideStringBuilder.Append(modifiedLine.Text);
                                // sideStringBuilder.AppendLine("</color>");
                                if (!modifiedDeletedStack.TryDequeue(out var modifiedDeleted))
                                {
                                    break;
                                }
                                
                                var modifiedChars = diffBuilder.BuildDiffModel(modifiedDeleted, modifiedLine.Text, true, false, new CharacterChunker());
                                var charsToDeleteIfBudgetIsEnough = new Stack<int>();
                                var charsToUninsertIfBudgetIsNotEnough = new Stack<int>();
                                foreach (var modifiedChar in modifiedChars.Lines)
                                {
                                    switch (modifiedChar.Type)
                                    {
                                        case DiffPlex.DiffBuilder.Model.ChangeType.Deleted:
                                            // sideSideStringBuilder.Append("<color=red>");
                                            // sideSideStringBuilder.Append(modifiedChar.Text);
                                            // sideSideStringBuilder.AppendLine("</color>");
                                            charsToDeleteIfBudgetIsEnough.Push(finalString.Length);
                                            finalString.Append(modifiedChar.Text);
                                            
                                            break;
                                        case DiffPlex.DiffBuilder.Model.ChangeType.Inserted:
                                            // sideSideStringBuilder.Append("<color=green>");
                                            // sideSideStringBuilder.Append(modifiedChar.Text);
                                            // sideSideStringBuilder.AppendLine("</color>");
                                            charsToUninsertIfBudgetIsNotEnough.Push(finalString.Length);
                                            finalString.Append(modifiedChar.Text);
                                            break;
                                        case DiffPlex.DiffBuilder.Model.ChangeType.Unchanged:
                                            // sideSideStringBuilder.Append("<color=black>");
                                            // sideSideStringBuilder.Append(modifiedChar.Text);
                                            // sideSideStringBuilder.AppendLine("</color>");
                                            finalString.Append(modifiedChar.Text);
                                            break;
                                    }
                                }

                                while (charsToDeleteIfBudgetIsEnough.Count > 0 && budget > 0)
                                {
                                    finalString.PopCharAndDecrementHigherIndices(charsToDeleteIfBudgetIsEnough);
                                    --budget;
                                }

                                while (charsToUninsertIfBudgetIsNotEnough.Count > 0)
                                {
                                    if (budget > 0)
                                    {
                                        charsToUninsertIfBudgetIsNotEnough.Pop();
                                        --budget;
                                    }
                                    else
                                    {
                                        finalString.PopCharAndDecrementHigherIndices(charsToUninsertIfBudgetIsNotEnough);
                                    }
                                }
                                
                                break;
                            case DiffPlex.DiffBuilder.Model.ChangeType.Unchanged:
                                // sideStringBuilder.Append("<color=black>");
                                // sideStringBuilder.Append(modifiedLine.Text);
                                // sideStringBuilder.AppendLine("</color>");
                                finalString.Append(modifiedLine.Text);
                                break;
                        }
                    }
                    finalString.AppendLine();
                    
                    break;
                case DiffPlex.DiffBuilder.Model.ChangeType.Unchanged:
                    // stringBuilder.Append("<color=black>");
                    // stringBuilder.Append(diffLine.Text);
                    // stringBuilder.AppendLine("</color>");
                    
                    finalString.AppendLine(diffLine.Text);
                    break;
            }
        }
        
        // finalString.AppendLine("-----------------------------------");
        // finalString.AppendLine(stringBuilder.ToString());
        // finalString.AppendLine("-----------------------------------");
        // finalString.AppendLine(sideStringBuilder.ToString());
        // finalString.AppendLine("-----------------------------------");
        // finalString.AppendLine(sideSideStringBuilder.ToString());

        return finalString.ToString();
    }
}