using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KeepCoding;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class ChaoticCountdownScript : ModuleScript
{
    private KMBombModule _Module;
    private System.Random _Rnd;

    [SerializeField]
    private KMSelectable _StartButton;
    [SerializeField]
    private KMSelectable[] _NumberButtons;
    [SerializeField]
    private KMSelectable[] _OperatorButtons;
    [SerializeField]
    private TextMesh _TargetNumber;
    [SerializeField]
    private TextMesh[] _Numbers;
    [SerializeField]
    private TextMesh[] _Equations;
    [SerializeField]
    private AudioClip[] _Sounds;
    [SerializeField]
    private AudioSource _Clock;

    private bool _isModuleSolved, _isSeedSet, _isTimerActive, _isOperatorAdded, _isFirstNumberDetermined;
    private int _seed, _pressedPosition = 13, _secondPressedPosition, _operatorPressedPosition = 4, _equationsUsed = 0, _depth;
    private ulong _result, _firstPress, _secondPress;
    private int[] _usableNumbers = new int[7];
    private List<int[]> _AllPermutations = new List<int[]>();
    private List<int[]> _AllOperations = new List<int[]>();
    private ulong[] _chosenNumbers = new ulong[7];
    private int[] _chosenOperations = new int[6];

    // Use this for initialization
    void Start()
    {
        for (int i = 0; i < _NumberButtons.Length; i++)
        {
            var x = i;
            _NumberButtons[i].Assign(onInteract: () => { NumberPress(x); });
        }

        for (int i = 0; i < _OperatorButtons.Length; i++)
        {
            var x = i;
            _OperatorButtons[i].Assign(onInteract: () => { OperatorPress(x); });
        }

        _StartButton.Assign(onInteract: () => { StartTimer(); });

        if (!_isSeedSet)
        {
            _seed = Rnd.Range(int.MinValue, int.MaxValue);
            Log("The seed is: " + _seed.ToString());
            _isSeedSet = true;
        }

        _Rnd = new System.Random(_seed);
        // SET SEED ABOVE IN CASE OF BUGS!!
        // _rnd = new System.Random(loggedSeed);
        _Module = Get<KMBombModule>();

        for (int i = 0; i < _usableNumbers.Length; i++)
        {
            _usableNumbers[i] = _Rnd.Next(1, 512);
            _Numbers[i].text = _usableNumbers[i].ToString();
        }

        for (int i = 0; i < _Equations.Length; i++)
            _Equations[i].text = "";

        for (int i = 0; i < 5040; i++)
        {
            int[] permutedNumbers = _usableNumbers.OrderBy(x => _Rnd.Next()).ToArray();
            if (!_AllPermutations.Contains(permutedNumbers))
                _AllPermutations.Add(permutedNumbers);
        }

        for(int i = 0; i < 4096; i++)
        {
            int[] permutedOperations = new int[6];
            for(int j = 0; j < permutedOperations.Length; j++)
                permutedOperations[j] = _Rnd.Next(0, 4);
            if(!_AllOperations.Contains(permutedOperations))
                _AllOperations.Add(permutedOperations);
        }

        int chosenNumberOrder = _Rnd.Next(0, 5040);
        for (int i = 0; i < _chosenNumbers.Length; i++)
            _chosenNumbers[i] = (ulong)_AllPermutations.ToArray()[chosenNumberOrder][i];

        tryAgain:
        int chosenOperationOrder = _Rnd.Next(0, 4096);
        for (int i = 0; i < _chosenOperations.Length; i++)
            _chosenOperations[i] = _AllOperations.ToArray()[chosenOperationOrder][i];

        _depth = _Rnd.Next(2, 6);

        _result = _chosenNumbers[0];
        for (int i = 0; i < _depth; i++)
            _result = Operate(_result, _chosenNumbers[i + 1], _chosenOperations[i]);

        if (_result > 999999 || _result < 0)
            goto tryAgain;

        _TargetNumber.text = _result.ToString();

        string chosenOperationLogging = "";
        for (int i = 0; i < _depth; i++)
        {
            switch (_chosenOperations[i])
            {
                case 0:
                    chosenOperationLogging += "+";
                    if (i != _depth - 1)
                        chosenOperationLogging += ", ";
                    break;
                case 1:
                    chosenOperationLogging += "-";
                    if (i != _depth - 1)
                        chosenOperationLogging += ", ";
                    break;
                case 2:
                    chosenOperationLogging += "×";
                    if (i != _depth - 1)
                        chosenOperationLogging += ", ";
                    break;
                case 3:
                    chosenOperationLogging += "÷";
                    if (i != _depth - 1)
                        chosenOperationLogging += ", ";
                    break;
            }
        }


        Log("The numbers on the module, in order, are: " + _usableNumbers.Join(", "));
        Log("The chosen number order is: " + _chosenNumbers.Join(", "));
        Log("The chosen operation order is: " + chosenOperationLogging);
        Log("As such, the target number is: " + _TargetNumber.text);
    }

    private ulong Operate(ulong a, ulong b, int op)
    {
        switch(op)
        {
            case 0:
                return (a + b);
            case 1:
                return (ulong)Math.Abs((decimal)a - b);
            case 2:
                return a * b;
            case 3:
                if (a % b == 0)
                    return a / b;
                else if (b % a == 0)
                    return b / a;
                else
                    return Math.Max(a, b) % Math.Min(a, b);
        }
        return 0;
    }

    private void NumberPress(int numberPosition)
    {
        if (_isModuleSolved || !_isTimerActive || numberPosition == _pressedPosition)
            return;
        ButtonEffect(_NumberButtons[numberPosition], 0.1f, KMSoundOverride.SoundEffect.ButtonPress);
        if (!_isOperatorAdded && _Numbers[numberPosition].text != "")
        {
            foreach (TextMesh number in _Numbers)
                number.color = new Color32(51, 51, 51, 255);
            _firstPress = ulong.Parse(_Numbers[numberPosition].text);
            _pressedPosition = numberPosition;
            _Numbers[numberPosition].color = new Color32(255, 51, 51, 255);
            _isFirstNumberDetermined = true;
        }
        else if (_isOperatorAdded && _Numbers[numberPosition].text != "")
        {
            _secondPress = ulong.Parse(_Numbers[numberPosition].text);
            _secondPressedPosition = numberPosition;
            _Numbers[7 + _equationsUsed].text = Operate(_firstPress, _secondPress, _operatorPressedPosition).ToString();
            _isOperatorAdded = false;
            _Numbers[_pressedPosition].text = "";
            _Numbers[_secondPressedPosition].text = "";
            _Numbers[_pressedPosition].color = new Color32(51, 51, 51, 255);
            _OperatorButtons[_operatorPressedPosition].GetComponentInChildren<TextMesh>().color = new Color32(51, 51, 51, 255);
            if (_Numbers[7 + _equationsUsed].text == _TargetNumber.text)
            {
                _isModuleSolved = true;
                StopCoroutine(ClockRoutine());
                _Clock.Stop();
                Log("You have made " + _TargetNumber.text + ". Module solved!");
                _Module.HandlePass();
                PlaySound(_Module.transform, false, _Sounds[0]);
            }
            _equationsUsed++;
            _isFirstNumberDetermined = false;
        }
    }

    private void OperatorPress(int operatorPosition)
    {
        if(_isFirstNumberDetermined && _operatorPressedPosition != operatorPosition)
        {
            if (_isOperatorAdded)
                _OperatorButtons[_operatorPressedPosition].GetComponentInChildren<TextMesh>().color = new Color32(51, 51, 51, 255);
            _operatorPressedPosition = operatorPosition;
            ButtonEffect(_OperatorButtons[_operatorPressedPosition], 0.1f, KMSoundOverride.SoundEffect.ButtonPress);
            _OperatorButtons[_operatorPressedPosition].GetComponentInChildren<TextMesh>().color = new Color32(255, 51, 51, 255);
            _isOperatorAdded = true;
        }
    }

    private void StartTimer()
    {
        if(!_isTimerActive)
        {
            ButtonEffect(_StartButton, 0.1f, KMSoundOverride.SoundEffect.ButtonPress);
            _Clock.Play();
            _isTimerActive = true;
            StartCoroutine(ClockRoutine());
            Log("The timer has been activated!");
        }
    }

    private IEnumerator ClockRoutine()
    {
        yield return new WaitForSeconds(31f);
        if(!_isModuleSolved)
        {
            Log("The time has run out!");
            _Module.HandleStrike();
            _Numbers[_pressedPosition].color = new Color32(51, 51, 51, 255);
            _OperatorButtons[_operatorPressedPosition].GetComponentInChildren<TextMesh>().color = new Color32(51, 51, 51, 255);
            _isSeedSet = false;
            _AllPermutations.Clear();
            _AllOperations.Clear();
            _pressedPosition = 13;
            _operatorPressedPosition = 4;
            _isFirstNumberDetermined = false;
            Start();
            _isTimerActive = false;
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} go/activate [Presses the blank square button] | !{0} 136 * 4128 [Performs the specified operation] | Commands are chainable with semicolons";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Replace(" ", "").Replace("×", "*").Replace("÷", "/").ToLower();
        string[] parameters = command.Split(';');
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].EqualsAny("go", "activate"))
            {
                yield return null;
                if (_isTimerActive)
                {
                    yield return "sendtochaterror The module has already been started!";
                    yield break;
                }
                yield return "strike";
                _StartButton.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else if (parameters[i].Contains("+") || parameters[i].Contains("-") || parameters[i].Contains("*") || parameters[i].Contains("/"))
            {
                string[] split = parameters[i].Split('+', '-', '*', '/');
                if (split.Length != 2)
                {
                    yield return "sendtochaterror!f The specified operation '" + parameters[i] + "' should include exactly two numbers and one operator!";
                    yield break;
                }
                ulong temp1;
                ulong temp2;
                if (!ulong.TryParse(split[0], out temp1) || !ulong.TryParse(split[1], out temp2))
                {
                    yield return "sendtochaterror!f The specified operation '" + parameters[i] + "' has at least one invalid number!";
                    yield break;
                }
                if (temp1 < 0 || temp2 < 0)
                {
                    yield return "sendtochaterror!f The specified operation '" + parameters[i] + "' has at least one invalid number!";
                    yield break;
                }
                if (!_isTimerActive)
                {
                    yield return "sendtochaterror The module must be started before operations can be performed!";
                    yield break;
                }
                bool found = false;
                for (int j = 0; j < _Numbers.Length; j++)
                {
                    if (_Numbers[j].text == split[0])
                    {
                        found = true;
                        _NumberButtons[j].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                }
                if (!found)
                {
                    yield return "sendtochaterror The specified operation '" + parameters[i] + "' has a number not present on the module!";
                    yield break;
                }
                char[] operators = { '+', '-', '*', '/' };
                for (int k = 0; k < operators.Length; k++)
                {
                    if (parameters[i].Contains(operators[k]))
                    {
                        _OperatorButtons[k].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                }
                found = false;
                for (int j = 0; j < _Numbers.Length; j++)
                {
                    if (_Numbers[j].text == split[1])
                    {
                        found = true;
                        _NumberButtons[j].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                }
                if (!found)
                {
                    yield return "sendtochaterror The specified operation '" + parameters[i] + "' has a number not present on the module!";
                    yield break;
                }
            }
            else
            {
                yield return "sendtochaterror!f The specified command '" + parameters[i] + "' is invalid!";
                yield break;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        bool bad = false;
        for (int i = 0; i < _Equations.Length; i++)
        {
            if (_Equations[i].text != "")
            {
                bad = true;
                break;
            }
        }
        if (_isTimerActive && (bad || _isFirstNumberDetermined))
        {
            _isModuleSolved = true;
            StopCoroutine(ClockRoutine());
            _Clock.Stop();
            _Module.HandlePass();
            yield break;
        }
        if (!_isTimerActive)
        {
            _StartButton.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        ulong prevUsed = _chosenNumbers[0];
        for (int i = 0; i < _depth; i++)
        {
            for (int j = 0; j < _Numbers.Length; j++)
            {
                if (_Numbers[j].text == prevUsed.ToString())
                {
                    _NumberButtons[j].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    break;
                }
            }
            _OperatorButtons[_chosenOperations[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
            for (int j = 0; j < _Numbers.Length; j++)
            {
                if (_Numbers[j].text == _chosenNumbers[i + 1].ToString())
                {
                    _NumberButtons[j].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    break;
                }
            }
            if (i != _depth - 1)
            {
                prevUsed = Operate(prevUsed, _chosenNumbers[i + 1], _chosenOperations[i]);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}