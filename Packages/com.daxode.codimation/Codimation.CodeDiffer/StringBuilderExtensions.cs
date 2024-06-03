using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class StringBuilderExtensions
{
    public static void PopCharAndDecrementHigherIndices(this StringBuilder stringBuilder, Stack<int> charsToDeleteIfBudgetIsEnough)
    {
        if (charsToDeleteIfBudgetIsEnough.Count == 0)
            return;
        
        var index = charsToDeleteIfBudgetIsEnough.Pop();
        stringBuilder.Remove(index, 1);
        
        var originalCharsToDeleteIfBudgetIsEnough = charsToDeleteIfBudgetIsEnough.ToList();
        charsToDeleteIfBudgetIsEnough.Clear();
        while (originalCharsToDeleteIfBudgetIsEnough.Count > 0)
        {
            var originalIndex = originalCharsToDeleteIfBudgetIsEnough[^1];
            originalCharsToDeleteIfBudgetIsEnough.RemoveAt(originalCharsToDeleteIfBudgetIsEnough.Count-1);
            if (originalIndex > index)
                charsToDeleteIfBudgetIsEnough.Push(originalIndex - 1);
            else
                charsToDeleteIfBudgetIsEnough.Push(originalIndex);
        }
    }
}