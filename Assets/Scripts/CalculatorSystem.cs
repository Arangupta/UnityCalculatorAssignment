using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CalculatorSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text expressionDisplay;
    [SerializeField] private TMP_Text resultDisplay;

    private string expression = "0";          // Expression used for evaluation
    private string displayExpression = "0";   // Expression shown on screen
    private bool justEvaluated = false;       // Tracks if last action was "="

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
    // Keyboard Input Handling
    // -----------------------------
    private void HandleKeyboardInput()
    {
        // Numbers (Alpha row)
        for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
            if (Input.GetKeyDown(key))
                AddInput(((int)key - (int)KeyCode.Alpha0).ToString());

        // Numbers (Keypad)
        for (KeyCode key = KeyCode.Keypad0; key <= KeyCode.Keypad9; key++)
            if (Input.GetKeyDown(key))
                AddInput(((int)key - (int)KeyCode.Keypad0).ToString());

        // Decimal point
        if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.KeypadPeriod))
            AddInput(".");

        // Operators
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus)) AddInput("+");
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) AddInput("-");
        if (Input.GetKeyDown(KeyCode.Asterisk) || Input.GetKeyDown(KeyCode.KeypadMultiply)) AddInput("*");
        if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide)) AddInput("/");

        // Equals / Enter
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Evaluate();

        // Clear (Backspace / Delete / Escape)
        if (Input.GetKeyDown(KeyCode.Backspace)) ClearLast();
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Escape)) ResetCalculator();
    }

    // -----------------------------
    // Input Handling
    // -----------------------------
    public void AddInput(string input)
    {
        // Reset state if last action was "="
        if (justEvaluated)
        {
            if (char.IsDigit(input[0]))
            {
                expression = "";
                displayExpression = "";
                resultDisplay.text = "";
            }
            expressionDisplay.text = "";
            justEvaluated = false;
        }

        // Prevent leading zeros (e.g. "0123")
        if (expression == "0" && char.IsDigit(input[0]))
        {
            expression = "";
            displayExpression = "";
        }

        // Map operators for display
        string displayValue = input;
        if (input == "*") displayValue = "×";
        else if (input == "/") displayValue = "÷";

        // Replace operator if last char was also an operator
        if ("+-*/".Contains(input))
        {
            if (expression.Length > 0 && "+-*/".Contains(expression[^1].ToString()))
            {
                expression = expression.Substring(0, expression.Length - 1) + input;
                displayExpression = displayExpression.Substring(0, displayExpression.Length - 1) + displayValue;

                expressionDisplay.text = displayExpression;
                return;
            }
        }

        // Append normally
        expression += input;
        displayExpression += displayValue;
        expressionDisplay.text = displayExpression;
    }

    private bool CanAddDecimal()
    {
        for (int i = expression.Length - 1; i >= 0; i--)
        {
            char c = expression[i];
            if (c == '.') return false;
            if ("+-*/".Contains(c.ToString())) break;
        }
        return true;
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
            expressionDisplay.text = "";
            resultDisplay.text = "Error";
            Debug.LogError($"Evaluation failed: {e.Message}");
            justEvaluated = true;
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
                // Handle unary +/-
                if ((c == '-' || c == '+') &&
                    (i == 0 || "+-*/".Contains(expr[i - 1].ToString())))
                {
                    numberBuffer += c;
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

        // Step 1: * and /
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
                    value = left * right;
                }
                else if (tokens[i] == "/")
                {
                    if (Math.Abs(right) < 0.000001)
                        value = 0;
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
