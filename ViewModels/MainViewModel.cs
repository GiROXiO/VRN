using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VRN.Models;
using VRN.Services;
using System.Net.Http;
using System.Text.Json;

namespace VRN.ViewModels;


public class MatrixRow
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}

public class CodewordRow
{
    public int Index { get; set; }
    public string Codeword { get; set; } = string.Empty;
}

public class PolynomialRow
{
    public int Index { get; set; }
    public string Polynomial { get; set; } = string.Empty;
}

// MainViewModel
public class MainViewModel : BaseViewModel
{
    // Navegacion
    private int _activePoint = 1;
    public int ActivePoint
    {
        get => _activePoint;
        set
        {
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

    private static readonly HttpClient _http = new();
    private CancellationTokenSource? _cts;

    // Inputs
    private string _inputN = "5";
    public string InputN { get => _inputN; set { SetField(ref _inputN, value); ValidateInputs(); } }

    private string _inputK = "2";
    public string InputK { get => _inputK; set { SetField(ref _inputK, value); ValidateInputs(); } }

    private string _inputQ = "7";
    public string InputQ { get => _inputQ; set { SetField(ref _inputQ, value); ValidateInputs(); } }

    // Validacion
    private string _validationMessage = string.Empty;
    public string ValidationMessage { get => _validationMessage; set => SetField(ref _validationMessage, value); }

    private bool _hasValidationError;
    public bool HasValidationError { get => _hasValidationError; set => SetField(ref _hasValidationError, value); }

    private bool _exceedsLimit;
    public bool ExceedsLimit { get => _exceedsLimit; set => SetField(ref _exceedsLimit, value); }

    // Estado
    private bool _isCalculating;
    public bool IsCalculating { get => _isCalculating; set { SetField(ref _isCalculating, value); OnPropertyChanged(nameof(IsIdle)); } }
    public bool IsIdle => !_isCalculating;

    private bool _showResults;
    public bool ShowResults { get => _showResults; set => SetField(ref _showResults, value); }

    // Resultados
    private CodeResult? _result;
    public CodeResult? Result { get => _result; set => SetField(ref _result, value); }

    private string _computeTimeText = string.Empty;
    public string ComputeTimeText { get => _computeTimeText; set => SetField(ref _computeTimeText, value); }

    private string _isRsText = string.Empty;
    public string IsRsText { get => _isRsText; set => SetField(ref _isRsText, value); }

    private string _crossVerifyText = string.Empty;
    public string CrossVerifyText { get => _crossVerifyText; set => SetField(ref _crossVerifyText, value); }

    // Animated metric counters
    private int _animMinDistance;
    public int AnimMinDistance { get => _animMinDistance; set => SetField(ref _animMinDistance, value); }

    private int _animSingleton;
    public int AnimSingleton { get => _animSingleton; set => SetField(ref _animSingleton, value); }

    private int _animCodewordCount;
    public int AnimCodewordCount { get => _animCodewordCount; set => SetField(ref _animCodewordCount, value); }

    private int _animPolynomialCount;
    public int AnimPolynomialCount { get => _animPolynomialCount; set => SetField(ref _animPolynomialCount, value); }

    // Matrix / list collections
    public ObservableCollection<MatrixRow> GeneratorMatrixRows { get; } = new();
    public ObservableCollection<MatrixRow> ParityMatrixRows { get; } = new();
    public ObservableCollection<MatrixRow> AdditionTableRows { get; } = new();
    public ObservableCollection<MatrixRow> MultiplicationTableRows { get; } = new();
    public ObservableCollection<CodewordRow> CodewordRows { get; } = new();
    public ObservableCollection<PolynomialRow> PolynomialRows { get; } = new();


    // Comandos
    public RelayCommand CalculateCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ToggleThemeCommand { get; } 

    private readonly ThemeService _themeService;

    public MainViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        CalculateCommand = new RelayCommand(_ => _ = CalculateAsync(), _ => IsIdle && !HasValidationError);
        CancelCommand = new RelayCommand(_ => _cts?.Cancel(), _ => IsCalculating);
        ToggleThemeCommand = new RelayCommand(_ => _themeService.Toggle());

        NavPoint1Command = new RelayCommand(_ => ActivePoint = 1);
        NavPoint2Command = new RelayCommand(_ => ActivePoint = 2);
        NavPoint3Command = new RelayCommand(_ => ActivePoint = 3);

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
            ValidationMessage = "N, K y Q deben ser enteros positivos.";
            HasValidationError = true;
            ExceedsLimit = false;
            return;
        }
        if (k > n)
        {
            ValidationMessage = "K debe ser ≤ N.";
            HasValidationError = true;
            ExceedsLimit = false;
            return;
        }

        HasValidationError = false;
        ExceedsLimit = false;
        ValidationMessage = string.Empty;
    }

    // Calculos
    private async Task CalculateAsync()
    {
        _cts = new CancellationTokenSource();
        IsCalculating = true;
        ShowResults = false;

        int n = int.Parse(InputN);
        int k = int.Parse(InputK);
        int q = int.Parse(InputQ);

        ClearResults();

        var sw = Stopwatch.StartNew();
        CodeResult? result = null;

        try
        {
            var url = $"http://localhost:8000/punto1?n={n}&k={k}&q={q}";
            var response = await _http.GetStringAsync(url);
            result = ParseApiResponse(response, n, k, q);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _ = ex; }
        finally
        {
            sw.Stop();
            IsCalculating = false;
        }

        if (result != null)
        {
            result.ComputationTimeMs = sw.ElapsedMilliseconds;
            PopulateResults(result);
            ShowResults = true;
        }
    }

    private CodeResult ParseApiResponse(string json, int n, int k, int q)
    {
        var result = new CodeResult();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Validación
        result.IsValid = true;
        result.ValidationMessages.Add(new ValidationMessage(MessageLevel.Ok,
            $"Parámetros válidos: RS({n},{k}) sobre GF({q})"));

        // Puntos de evaluación
        result.EvaluationPoints = root.GetProperty("puntos_evaluacion")
            .EnumerateArray()
            .Select(x => x.GetInt32())
            .ToArray();

        // Polinomios
        result.Polynomials = root.GetProperty("polinomios")
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToList();
        result.PolynomialCount = root.GetProperty("total_polinomios").GetInt32();

        // Palabras código
        result.Codewords = root.GetProperty("palabras_codigo")
            .EnumerateArray()
            .Select(arr => arr.EnumerateArray().Select(x => x.GetInt32()).ToArray())
            .ToList();
        result.CodewordCount = root.GetProperty("total_palabras").GetInt32();

        // Matriz generadora
        var gRows = root.GetProperty("matriz_G").EnumerateArray().ToList();
        result.GeneratorMatrix = new int[gRows.Count, n];
        for (int i = 0; i < gRows.Count; i++)
        {
            var cols = gRows[i].EnumerateArray().ToList();
            for (int j = 0; j < cols.Count; j++)
                result.GeneratorMatrix[i, j] = cols[j].GetInt32();
        }

        // Matriz de paridad
        var hRows = root.GetProperty("matriz_H").EnumerateArray().ToList();
        result.ParityCheckMatrix = new int[hRows.Count, n];
        for (int i = 0; i < hRows.Count; i++)
        {
            var cols = hRows[i].EnumerateArray().ToList();
            for (int j = 0; j < cols.Count; j++)
                result.ParityCheckMatrix[i, j] = cols[j].GetInt32();
        }

        // Métricas
        result.MinimumDistance = root.GetProperty("distancia_minima").GetInt32();
        result.SingletonBound = root.GetProperty("cota_singleton").GetInt32();
        result.IsRS = root.GetProperty("es_rs").GetBoolean();
        result.AllCodewordsValid = root.GetProperty("palabras_validas").GetBoolean();
        result.CrossVerificationPassed = root.GetProperty("verificacion_cruzada").GetBoolean();

        // Tablas del campo
        var addRows = root.GetProperty("tabla_adicion").EnumerateArray().ToList();
        result.AdditionTable = new int[q, q];
        for (int i = 0; i < addRows.Count; i++)
        {
            var cols = addRows[i].EnumerateArray().ToList();
            for (int j = 0; j < cols.Count; j++)
                result.AdditionTable[i, j] = cols[j].GetInt32();
        }

        var mulRows = root.GetProperty("tabla_multiplicacion").EnumerateArray().ToList();
        result.MultiplicationTable = new int[q, q];
        for (int i = 0; i < mulRows.Count; i++)
        {
            var cols = mulRows[i].EnumerateArray().ToList();
            for (int j = 0; j < cols.Count; j++)
                result.MultiplicationTable[i, j] = cols[j].GetInt32();
        }

        return result;
    }

    private void PopulateResults(CodeResult r)
    {
        Result = r;
        ComputeTimeText = $"Calculado en {r.ComputationTimeMs} ms";
        IsRsText = r.IsRS ? "Sí ✓" : "No";
        CrossVerifyText = r.CrossVerificationPassed ? "✓ Pasó" : "✗ Falló";

        if (r.GeneratorMatrix != null) PopulateMatrix(r.GeneratorMatrix, GeneratorMatrixRows);
        if (r.ParityCheckMatrix != null) PopulateMatrix(r.ParityCheckMatrix, ParityMatrixRows);
        if (r.AdditionTable != null) PopulateMatrix(r.AdditionTable, AdditionTableRows);
        if (r.MultiplicationTable != null) PopulateMatrix(r.MultiplicationTable, MultiplicationTableRows);

        int maxCW = Math.Min(r.Codewords.Count, 5000);
        for (int i = 0; i < maxCW; i++)
            CodewordRows.Add(new CodewordRow
            {
                Index = i + 1,
                Codeword = $"({string.Join(", ", r.Codewords[i])})"
            });

        int maxPoly = Math.Min(r.Polynomials.Count, 5000);
        for (int i = 0; i < maxPoly; i++)
            PolynomialRows.Add(new PolynomialRow
            {
                Index = i + 1,
                Polynomial = r.Polynomials[i]
            });

        _ = AnimateCountUp(r.MinimumDistance, v => AnimMinDistance = v);
        _ = AnimateCountUp(r.SingletonBound, v => AnimSingleton = v);
        _ = AnimateCountUp(r.CodewordCount, v => AnimCodewordCount = v);
        _ = AnimateCountUp(r.PolynomialCount, v => AnimPolynomialCount = v);
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
            double t = (double)i / frames;
            double eased = 1 - Math.Pow(1 - t, 3); // ease-out cubic
            setter((int)(target * eased));
            await Task.Delay(16);
        }
        setter(target);
    }
}