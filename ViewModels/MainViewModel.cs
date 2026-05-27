using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VRN.Core;
using VRN.Models;
using VRN.Services;

namespace VRN.ViewModels;

// Progreso Observable
public class ProgressLine : BaseViewModel
{
    public string StepId { get; init; } = string.Empty;
    public string Label  { get; init; } = string.Empty;

    private bool _isRunning;
    public bool IsRunning  { get => _isRunning;  set => SetField(ref _isRunning,  value); }

    private bool _isCompleted;
    public bool IsCompleted { get => _isCompleted; set => SetField(ref _isCompleted, value); }

    private bool _isFailed;
    public bool IsFailed   { get => _isFailed;   set => SetField(ref _isFailed,   value); }
}

public class MatrixRow
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}

public class CodewordRow
{
    public int    Index    { get; set; }
    public string Codeword { get; set; } = string.Empty;
}

public class PolynomialRow
{
    public int    Index      { get; set; }
    public string Polynomial { get; set; } = string.Empty;
}

// MainViewModel
public class MainViewModel : BaseViewModel
{
    // Navegacion
    private int _activePoint = 1;
    public int ActivePoint{
        get => _activePoint;
        set{
            SetField(ref _activePoint, value);
            OnPropertyChanged(nameof(IsPoint1));
            OnPropertyChanged(nameof(IsPoint2));
            OnPropertyChanged(nameof(IsPoint3));
        }
    }
    
    public bool IsPoint1 => ActivePoint == 1;
    public bool IsPoint2 => ActivePoint == 2;
    public bool IsPoint3 => ActivePoint == 3;

    public RelayCommand NavPoint1Command { get; }
    public RelayCommand NavPoint2Command { get; }
    public RelayCommand NavPoint3Command { get; }

    // Definicion del paso a paso de la aplicacion
    private static readonly (string Id, string Label)[] StepDefs =
    {
        ("validate",    "Validando parámetros"),
        ("field",       "Construyendo campo GF(q)"),
        ("polynomials", "Generando polinomios"),
        ("codewords",   "Generando palabras código"),
        ("matrixG",     "Construyendo matriz generadora G"),
        ("matrixH",     "Construyendo matriz de paridad H"),
        ("verify",      "Verificando palabras código"),
        ("distance",    "Calculando distancia mínima"),
        ("crossverify", "Verificación cruzada"),
    };

    private readonly CodeEngine    _engine  = new();
    private readonly HistoryService _history = new();
    private CancellationTokenSource? _cts;

    // Inputs
    private string _inputN = "5";
    public  string InputN  { get => _inputN; set { SetField(ref _inputN, value); ValidateInputs(); } }

    private string _inputK = "2";
    public  string InputK  { get => _inputK; set { SetField(ref _inputK, value); ValidateInputs(); } }

    private string _inputQ = "7";
    public  string InputQ  { get => _inputQ; set { SetField(ref _inputQ, value); ValidateInputs(); } }

    // Validacion
    private string _validationMessage = string.Empty;
    public  string ValidationMessage  { get => _validationMessage; set => SetField(ref _validationMessage, value); }

    private bool _hasValidationError;
    public  bool HasValidationError   { get => _hasValidationError; set => SetField(ref _hasValidationError, value); }

    private bool _exceedsLimit;
    public  bool ExceedsLimit         { get => _exceedsLimit; set => SetField(ref _exceedsLimit, value); }

    // Estado
    private bool _isCalculating;
    public  bool IsCalculating { get => _isCalculating; set { SetField(ref _isCalculating, value); OnPropertyChanged(nameof(IsIdle)); } }
    public  bool IsIdle => !_isCalculating;

    private bool _showProgress;
    public  bool ShowProgress { get => _showProgress; set => SetField(ref _showProgress, value); }

    private bool _showResults;
    public  bool ShowResults  { get => _showResults;  set => SetField(ref _showResults,  value); }

    // Progreso
    public ObservableCollection<ProgressLine> ProgressLines { get; } = new();

    private int _progressPercent;
    public  int ProgressPercent { get => _progressPercent; set => SetField(ref _progressPercent, value); }

    private int _completedStepCount;
    private int TotalStepCount => StepDefs.Length;

    private string _progressLabel = string.Empty;
    public  string ProgressLabel  { get => _progressLabel; set => SetField(ref _progressLabel, value); }

    // Resultados
    private CodeResult? _result;
    public  CodeResult? Result { get => _result; set => SetField(ref _result, value); }

    private string _computeTimeText = string.Empty;
    public  string ComputeTimeText  { get => _computeTimeText; set => SetField(ref _computeTimeText, value); }

    private string _isRsText        = string.Empty;
    public  string IsRsText         { get => _isRsText;        set => SetField(ref _isRsText,        value); }

    private string _crossVerifyText = string.Empty;
    public  string CrossVerifyText  { get => _crossVerifyText; set => SetField(ref _crossVerifyText, value); }

    // Animated metric counters
    private int _animMinDistance;
    public  int AnimMinDistance    { get => _animMinDistance;    set => SetField(ref _animMinDistance,    value); }

    private int _animSingleton;
    public  int AnimSingleton      { get => _animSingleton;      set => SetField(ref _animSingleton,      value); }

    private int _animCodewordCount;
    public  int AnimCodewordCount  { get => _animCodewordCount;  set => SetField(ref _animCodewordCount,  value); }

    private int _animPolynomialCount;
    public  int AnimPolynomialCount { get => _animPolynomialCount; set => SetField(ref _animPolynomialCount, value); }

    // Matrix / list collections
    public ObservableCollection<MatrixRow>     GeneratorMatrixRows    { get; } = new();
    public ObservableCollection<MatrixRow>     ParityMatrixRows       { get; } = new();
    public ObservableCollection<MatrixRow>     AdditionTableRows      { get; } = new();
    public ObservableCollection<MatrixRow>     MultiplicationTableRows{ get; } = new();
    public ObservableCollection<CodewordRow>   CodewordRows           { get; } = new();
    public ObservableCollection<PolynomialRow> PolynomialRows         { get; } = new();

    // Historial
    public HistoryViewModel History { get; } = new();

    // Comandos
    public RelayCommand CalculateCommand      { get; }
    public RelayCommand CancelCommand         { get; }
    public RelayCommand ToggleThemeCommand    { get; }
    public RelayCommand LoadFromHistoryCommand{ get; }

    private readonly ThemeService _themeService;

    public MainViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        CalculateCommand       = new RelayCommand(_ => _ = CalculateAsync(), _ => IsIdle && !HasValidationError);
        CancelCommand          = new RelayCommand(_ => _cts?.Cancel(),        _ => IsCalculating);
        ToggleThemeCommand     = new RelayCommand(_ => _themeService.Toggle());
        LoadFromHistoryCommand = new RelayCommand(LoadFromHistory);

        NavPoint1Command = new RelayCommand(_ => ActivePoint = 1);
        NavPoint2Command = new RelayCommand(_ => ActivePoint = 2);
        NavPoint3Command = new RelayCommand(_ => ActivePoint = 3);

        _engine.OnProgress += OnEngineProgress;
        ValidateInputs();
    }

    // Validacion
    private void ValidateInputs()
    {
        bool nOk = int.TryParse(InputN, out int n) && n >= 1;
        bool kOk = int.TryParse(InputK, out int k) && k >= 1;
        bool qOk = int.TryParse(InputQ, out int q) && q >= 2;

        if (!nOk || !kOk || !qOk)
        {
            ValidationMessage  = "N, K y Q deben ser enteros positivos.";
            HasValidationError = true;
            ExceedsLimit       = false;
            return;
        }
        if (k > n)
        {
            ValidationMessage  = "K debe ser ≤ N.";
            HasValidationError = true;
            ExceedsLimit       = false;
            return;
        }
        var (isPrime, _, _) = FiniteFieldMath.IsPrimePower(q);
        if (!isPrime)
        {
            ValidationMessage  = $"Q = {q} no es una potencia de primo.";
            HasValidationError = true;
            ExceedsLimit       = false;
            return;
        }

        bool exceeds  = CodeEngine.WouldExceedLimit(q, k);
        ExceedsLimit  = exceeds;
        HasValidationError = false;
        ValidationMessage  = exceeds
            ? $"⚠  q^k = {q}^{k} supera 50 000 — puede ser lento."
            : string.Empty;
    }

    // Calculos
    private async Task CalculateAsync()
    {
        _cts = new CancellationTokenSource();
        IsCalculating    = true;
        ShowProgress     = true;
        ShowResults      = false;
        _completedStepCount = 0;
        ProgressPercent  = 0;

        // Parseo de inputs
        int n = int.Parse(InputN);
        int k = int.Parse(InputK);
        int q = int.Parse(InputQ);
        ProgressLabel = $"RS({n},{k}) sobre GF({q})";

        ProgressLines.Clear();
        foreach (var (id, label) in StepDefs)
            ProgressLines.Add(new ProgressLine { StepId = id, Label = label });

        ClearResults();

        var parameters = new CodeParameters(n, k, q);
        var sw = Stopwatch.StartNew();
        CodeResult? result = null;
        try
        {
            result = await _engine.ComputeAsync(parameters, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            MarkAllRunningAsFailed();
        }
        catch (Exception ex)
        {
            MarkAllRunningAsFailed();
            _ = ex; // logged implicitly via step states
        }
        finally
        {
            sw.Stop();
            IsCalculating = false;
        }

        if (result != null)
        {
            result.ComputationTimeMs = sw.ElapsedMilliseconds;

            History.AddEntry(new HistoryEntry { Parameters = parameters, Result = result });

            await Task.Delay(600);
            PopulateResults(result);
            ShowProgress = false;
            ShowResults  = true;
        }
    }

    private void OnEngineProgress(CalculationStep step)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var line = ProgressLines.FirstOrDefault(l => l.StepId == step.StepId);
            if (line == null) return;

            switch (step.Status)
            {
                case CalculationStatus.Running:
                    line.IsRunning   = true;
                    line.IsCompleted = false;
                    line.IsFailed    = false;
                    break;

                case CalculationStatus.Completed:
                    line.IsRunning   = false;
                    line.IsCompleted = true;
                    _completedStepCount++;
                    ProgressPercent = (int)(_completedStepCount * 100.0 / TotalStepCount);
                    break;

                case CalculationStatus.Failed:
                    line.IsRunning = false;
                    line.IsFailed  = true;
                    break;
            }
        });
    }

    private void MarkAllRunningAsFailed()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var l in ProgressLines.Where(l => l.IsRunning))
            {
                l.IsRunning = false;
                l.IsFailed  = true;
            }
        });
    }

    private void PopulateResults(CodeResult r)
    {
        Result          = r;
        ComputeTimeText = $"Calculado en {r.ComputationTimeMs} ms";
        IsRsText        = r.IsRS ? "Sí ✓" : "No";
        CrossVerifyText = r.CrossVerificationPassed ? "✓ Pasó" : "✗ Falló";

        if (r.GeneratorMatrix   != null) PopulateMatrix(r.GeneratorMatrix,    GeneratorMatrixRows);
        if (r.ParityCheckMatrix != null) PopulateMatrix(r.ParityCheckMatrix,  ParityMatrixRows);
        if (r.AdditionTable     != null) PopulateMatrix(r.AdditionTable,      AdditionTableRows);
        if (r.MultiplicationTable != null) PopulateMatrix(r.MultiplicationTable, MultiplicationTableRows);

        int maxCW = Math.Min(r.Codewords.Count, 5000);
        for (int i = 0; i < maxCW; i++)
            CodewordRows.Add(new CodewordRow
            {
                Index    = i + 1,
                Codeword = $"({string.Join(", ", r.Codewords[i])})"
            });

        int maxPoly = Math.Min(r.Polynomials.Count, 5000);
        for (int i = 0; i < maxPoly; i++)
            PolynomialRows.Add(new PolynomialRow
            {
                Index      = i + 1,
                Polynomial = r.Polynomials[i]
            });

        _ = AnimateCountUp(r.MinimumDistance,   v => AnimMinDistance    = v);
        _ = AnimateCountUp(r.SingletonBound,    v => AnimSingleton      = v);
        _ = AnimateCountUp(r.CodewordCount,     v => AnimCodewordCount  = v);
        _ = AnimateCountUp(r.PolynomialCount,   v => AnimPolynomialCount = v);
    }

    private void ClearResults()
    {
        GeneratorMatrixRows.Clear();
        ParityMatrixRows.Clear();
        AdditionTableRows.Clear();
        MultiplicationTableRows.Clear();
        CodewordRows.Clear();
        PolynomialRows.Clear();
        AnimMinDistance = AnimSingleton = AnimCodewordCount = AnimPolynomialCount = 0;
    }

    private static void PopulateMatrix(int[,] m, ObservableCollection<MatrixRow> target)
    {
        int rows = m.GetLength(0);
        int cols = m.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            var cells = new string[cols];
            for (int j = 0; j < cols; j++)
                cells[j] = m[i, j].ToString();
            target.Add(new MatrixRow { Cells = cells });
        }
    }

    private static async Task AnimateCountUp(int target, Action<int> setter)
    {
        if (target == 0) { setter(0); return; }
        const int frames = 30;
        for (int i = 1; i <= frames; i++)
        {
            double t      = (double)i / frames;
            double eased  = 1 - Math.Pow(1 - t, 3); // ease-out cubic
            setter((int)(target * eased));
            await Task.Delay(16);
        }
        setter(target);
    }

    private void LoadFromHistory(object? _)
    {
        if (History.Selected == null) return;
        var e = History.Selected;
        InputN = e.Parameters.N.ToString();
        InputK = e.Parameters.K.ToString();
        InputQ = e.Parameters.Q.ToString();
    }
}