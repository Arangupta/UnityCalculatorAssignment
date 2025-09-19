using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CalculatorSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text expressionDisplay;
    [SerializeField] private TMP_Text resultDisplay;

    private string expression = "0";          
    private string displayExpression = "0";   
    private bool justEvaluated = false;

    private void Start()
    {
        expressionDisplay.text = displayExpression;
        resultDisplay.text = "";
    }   

    private void Update()
    {
        HandleKeyboardInput();
    }

    // -----------------------------
    // Keyboard Input
    // -----------------------------
    private void HandleKeyboardInput()
    {
        // Digits 0-9
        for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
            if (Input.GetKeyDown(key))
                AddInput(((int)key - (int)KeyCode.Alpha0).ToString());

        for (KeyCode key = KeyCode.Keypad0; key <= KeyCode.Keypad9; key++)
            if (Input.GetKeyDown(key))
                AddInput(((int)key - (int)KeyCode.Keypad0).ToString());

        // Decimal
        if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.KeypadPeriod))
            AddInput(".");

        // Operators
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            AddInput("+");

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            AddInput("-");

        if (Input.GetKeyDown(KeyCode.Asterisk) || Input.GetKeyDown(KeyCode.KeypadMultiply))
            AddInput("*");

        if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide))
            AddInput("/");

        // Equals or Enter (WebGL-safe)
        if (Input.GetKeyDown(KeyCode.Equals) || 
            Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.KeypadEnter) || 
            Input.GetKeyDown(KeyCode.KeypadEquals) || 
            Input.inputString == "\n" || 
            Input.inputString == "=")
        {
            Evaluate();
        }

        // Backspace
        if (Input.GetKeyDown(KeyCode.Backspace))
            ClearLast();

        // Reset
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Escape))
            ResetCalculator();
    }

    // -----------------------------
    // Input Handling
    // -----------------------------
    public void AddInput(string input)
    {
        // If last action was "=", decide what to do
        if (justEvaluated)
        {
            if (char.IsDigit(input[0]))
            {
                // Start new calculation if number pressed
                expression = "";
                displayExpression = "";
                resultDisplay.text = "";
            }
            // If operator pressed, continue from result
            expressionDisplay.text = "";
            justEvaluated = false;
        }

        // Replace starting 0 if digit pressed
        if (expression == "0" && char.IsDigit(input[0]))
        {
            expression = "";
            displayExpression = "";
        }

        string displayValue = input;
        string evalValue = input;

        if (input == "*") displayValue = "×";
        else if (input == "/") displayValue = "÷";

        // ✅ Replace operator if last char is already an operator
        if ("+-*/".Contains(evalValue))
        {
            if (expression.Length > 0 && "+-*/".Contains(expression[^1].ToString()))
            {
                // Replace in both internal and display strings
                expression = expression.Substring(0, expression.Length - 1) + evalValue;
                displayExpression = displayExpression.Substring(0, displayExpression.Length - 1) + displayValue;
                expressionDisplay.text = displayExpression;
                return;
            }
        }

        expression += evalValue;
        displayExpression += displayValue;
        expressionDisplay.text = displayExpression;
    }

    public void ClearLast()
    {
        if (justEvaluated)
        {
            ResetCalculator();
            return;
        }

        if (expression.Length > 0)
        {
            expression = expression.Substring(0, expression.Length - 1);
            displayExpression = displayExpression.Substring(0, displayExpression.Length - 1);

            if (expression == "")
            {
                expression = "0";
                displayExpression = "0";
            }

            expressionDisplay.text = displayExpression;
        }
    }

    public void ResetCalculator()
    {
        expression = "0";
        displayExpression = "0";
        expressionDisplay.text = displayExpression;
        resultDisplay.text = "";
        justEvaluated = false;
    }

    // -----------------------------
    // Evaluation
    // -----------------------------
    public void Evaluate()
    {
        try
        {
            if (string.IsNullOrEmpty(expression)) return;

            // ❌ If expression ends with an operator → show error
            if ("+-*/".Contains(expression[^1].ToString()))
            {
                resultDisplay.text = "Error";
                justEvaluated = true;
                return;
            }

            double result = EvaluateExpression(expression);

            expressionDisplay.text = "";
            resultDisplay.text = (Math.Abs(result % 1) < 0.000001)
                ? ((int)result).ToString()
                : result.ToString("0.######");

            // Save result for chaining
            expression = result.ToString();
            displayExpression = result.ToString();
            justEvaluated = true;
        }
        catch (Exception e)
        {
            resultDisplay.text = "Error";
            Debug.LogError($"Evaluation failed: {e.Message}");
        }
    }

    // -----------------------------
    // Tokenization
    // -----------------------------
    private List<string> Tokenize(string expr)
    {
        List<string> tokens = new List<string>();
        string numberBuffer = "";

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];

            if (char.IsDigit(c) || c == '.')
            {
                numberBuffer += c;
            }
            else
            {
                if ((c == '-' || c == '+') &&
                    (i == 0 || "+-*/".Contains(expr[i - 1].ToString())))
                {
                    numberBuffer += c; // unary sign
                }
                else
                {
                    if (numberBuffer != "")
                    {
                        tokens.Add(numberBuffer);
                        numberBuffer = "";
                    }
                    tokens.Add(c.ToString());
                }
            }
        }

        if (numberBuffer != "")
            tokens.Add(numberBuffer);

        return tokens;
    }

    // -----------------------------
    // Evaluation Logic (DMAS)
    // -----------------------------
    private double EvaluateExpression(string expr)
    {
        List<string> tokens = Tokenize(expr);

        // Step 1: × and ÷
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == "*" || tokens[i] == "/")
            {
                if (i == 0 || i == tokens.Count - 1)
                    throw new Exception("Invalid expression");

                double left = double.Parse(tokens[i - 1]);
                double right = double.Parse(tokens[i + 1]);

                double value = 0;

                if (tokens[i] == "*")
                {
                    value = left * right; // normal multiply
                }
                else if (tokens[i] == "/")
                {
                    if (Math.Abs(right) < 0.000001)
                        value = 0; // ✅ return 0 instead of error
                    else
                        value = left / right;
                }

                tokens[i - 1] = value.ToString();
                tokens.RemoveAt(i);
                tokens.RemoveAt(i);
                i--;
            }
        }

        // Step 2: + and -
        double result = double.Parse(tokens[0]);
        for (int i = 1; i < tokens.Count; i += 2)
        {
            string op = tokens[i];
            double next = double.Parse(tokens[i + 1]);

            if (op == "+") result += next;
            else if (op == "-") result -= next;
        }

        return result;
    }
}
