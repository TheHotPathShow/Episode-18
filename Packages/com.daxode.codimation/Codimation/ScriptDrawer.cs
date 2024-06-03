using Codimation.TreeSitter;
using UnityEngine;

public class ScriptDrawer : MonoBehaviour
{
    TMPro.TMP_Text m_Text;
    
    [SerializeField]
    float Time = 10.0f;
    float m_TimeLeft;

    [SerializeField] [Multiline(10)]
    string SourceA = @"interface IState<TEnum> where TEnum : Enum {
    public void Enter(StateManager<TEnum> manager){}
    public void Update(StateManager<TEnum> manager){}
    public void Exit(StateManager<TEnum> manager){}
}";
    [SerializeField] [Multiline(10)]
    string SourceB = @"interface IState<TEnum> where TEnum : Enum {
    public void Enter(StateManager<TEnum> manager){}
    public void Update(StateManager<TEnum> manager)
    {
        manager.ChangeState(default);
    }
    public void Exit(StateManager<TEnum> manager){}
}";
    
    bool m_IsSourceA = true;
    
    // Start is called before the first frame update
    void Start()
    {
        var highlightedString = TreeSitterUtility.HighlightWithLineNumbers(SourceA);
        m_Text = GetComponent<TMPro.TMP_Text>();
        m_Text.text = highlightedString;
    }
    
    [SerializeField]
    int Budget = 5;
    
    void Update()
    {
        var budgetLeft = Budget;
        var finalString = CodeDifferUtilities.CodeLerpUsingBudget(SourceA, SourceB, ref budgetLeft);
        if (m_Text.text != finalString)
        {
            m_Text.text = TreeSitterUtility.HighlightWithLineNumbers(finalString);
        }
        
        m_TimeLeft -= UnityEngine.Time.deltaTime;
        if (m_TimeLeft < 0)
        {
            if (m_IsSourceA)
            {
                m_TimeLeft = 4;
                m_IsSourceA = false;
                Budget = 0;
            }
            else if (budgetLeft > 0)
            {
                m_TimeLeft = 4;
                m_IsSourceA = true;
            }
            else
            {
                m_TimeLeft = Time;
                Budget++;
            }
        }
    }
}