import math
import sympy as sp
from itertools import product

# ── Aritmética en GF(q) ───────────────────────────────────────────────────────

def es_primo(n):
    if n < 2: return False
    if n == 2: return True
    if n % 2 == 0: return False
    for i in range(3, int(math.sqrt(n)) + 1, 2):
        if n % i == 0: return False
    return True

def es_potencia_primo(q):
    """Retorna (es_potencia_primo, p, m)"""
    if q < 2: return (False, 0, 0)
    if es_primo(q): return (True, q, 1)
    limite = int(math.sqrt(q))
    for p in range(2, limite + 1):
        if not es_primo(p): continue
        val, m = q, 0
        while val > 1 and val % p == 0:
            val //= p
            m += 1
        if val == 1 and m >= 1:
            return (True, p, m)
    return (False, 0, 0)

def modulo(a, q):
    return ((a % q) + q) % q

def suma_modular(a, b, q):
    return ((a + b) % q + q) % q

def multiplicacion_modular(a, b, q):
    return ((a * b) % q + q) % q

def potencia_modular(base, exp, q):
    if q == 1: return 0
    return pow(base, exp, q)

def euclides_extendido(a, b):
    if b == 0: return (a, 1, 0)
    g, x1, y1 = euclides_extendido(b, a % b)
    return (g, y1, x1 - (a // b) * y1)

def inverso_modular(a, q):
    a = modulo(a, q)
    mcd, x, _ = euclides_extendido(a, q)
    if mcd != 1:
        raise ArithmeticError(f"No existe inverso de {a} mod {q}")
    return modulo(x, q)

def tabla_adicion(q):
    return [[(i + j) % q for j in range(q)] for i in range(q)]

def tabla_multiplicacion(q):
    return [[(i * j) % q for j in range(q)] for i in range(q)]

# Polinomios sobre GF(q) 

def generar_coeficientes(q, k):
    return [list(c) for c in product(range(q), repeat=k)]

def evaluar_horner(coeficientes, x, q):
    """Evalúa p(x) usando el método de Horner sobre GF(q)"""
    if not coeficientes: return 0
    resultado = coeficientes[-1]
    for i in range(len(coeficientes) - 2, -1, -1):
        resultado = suma_modular(multiplicacion_modular(resultado, x, q), coeficientes[i], q)
    return modulo(resultado, q)

def formatear_polinomio(coeficientes):
    """Formatea un vector de coeficientes como string legible. Ej: [3,2,1] → '3 + 2x + x^2'"""
    terminos = []
    for i, c in enumerate(coeficientes):
        if c == 0: continue
        if i == 0:
            terminos.append(str(c))
        elif i == 1:
            terminos.append('x' if c == 1 else f'{c}x')
        else:
            terminos.append(f'x^{i}' if c == 1 else f'{c}x^{i}')
    
    return ' + '.join(terminos) if terminos else '0'

def generar_palabras_codigo(puntos_evaluacion, coeficientes, q):
    """Genera todas las palabras código evaluando cada polinomio en los puntos dados"""
    palabras = []
    vistas = set()
    for coef in coeficientes:
        palabra = tuple(evaluar_horner(coef, a, q) for a in puntos_evaluacion)
        if palabra not in vistas:
            vistas.add(palabra)
            palabras.append(list(palabra))
    return palabras

# Matrices sobre GF(q)

def matriz_generadora(puntos_evaluacion, k, q):
    """Construye la matriz de Vandermonde G donde G[i,j] = puntos[j]^i mod q"""
    n = len(puntos_evaluacion)
    return [[potencia_modular(puntos_evaluacion[j], i, q) for j in range(n)] for i in range(k)]

def transponer(M):
    return [list(fila) for fila in zip(*M)]

def multiplicar_matrices(A, B, q):
    """Multiplica dos matrices sobre GF(q)"""
    filas_A, cols_A = len(A), len(A[0])
    filas_B, cols_B = len(B), len(B[0])
    if cols_A != filas_B:
        raise ValueError(f"Dimensiones incompatibles: {filas_A}×{cols_A} y {filas_B}×{cols_B}")
    return [[modulo(sum(A[i][k] * B[k][j] for k in range(cols_A)), q) for j in range(cols_B)] for i in range(filas_A)]

def matriz_paridad(G, q):
    def a_mod_q(val):
        num = int(val.p)
        den = int(val.q)
        return (num * pow(den, -1, q)) % q

    k = len(G)
    n = len(G[0])
    M = sp.Matrix(G)
    rref, columnas_pivote = M.rref()
    rref = rref.applyfunc(lambda x: a_mod_q(x))

    columnas_libres = [c for c in range(n) if c not in columnas_pivote]
    if not columnas_libres:
        return None

    H = []
    for col_libre in columnas_libres:
        fila = [0] * n
        fila[col_libre] = 1
        for fila_idx, col_pivote in enumerate(columnas_pivote):
            fila[col_pivote] = modulo(-int(rref[fila_idx, col_libre]), q)
        H.append(fila)
    return H

def distancia_minima(palabras_codigo):
    """Calcula la distancia mínima de Hamming entre todas las palabras código"""
    if len(palabras_codigo) < 2: return 0
    n = len(palabras_codigo[0])
    d_min = n + 1
    for i in range(len(palabras_codigo)):
        for j in range(i + 1, len(palabras_codigo)):
            dist = sum(1 for pos in range(n) if palabras_codigo[i][pos] != palabras_codigo[j][pos])
            if dist < d_min:
                d_min = dist
            if d_min == 1: return 1
    return d_min if d_min <= n else 0

def verificar_palabras_codigo(palabras_codigo, H, q):
    """Verifica que todas las palabras código cumplen c·Hᵀ ≡ 0 (mod q)"""
    Ht = transponer(H)
    for c in palabras_codigo:
        for fila in H:
            if modulo(sum(c[j] * fila[j] for j in range(len(c))), q) != 0:
                return False
    return True

def verificacion_cruzada(palabras_codigo, G, q, k):
    """Verifica que m·G genera el mismo conjunto de palabras código"""
    n = len(G[0])
    conjunto_polinomios = set(tuple(c) for c in palabras_codigo)
    for coef in generar_coeficientes(q, k):
        palabra = tuple(modulo(sum(coef[i] * G[i][j] for i in range(k)), q) for j in range(n))
        if palabra not in conjunto_polinomios:
            return False
    return True

# ── Orquestador principal ─────────────────────────────────────────────────────

def calcular(n, k, q):
    """Pipeline completo del código Reed-Solomon RS(n,k) sobre GF(q)"""
    resultado = {}

    # 1. Validar parámetros
    es_potencia, p, m = es_potencia_primo(q)
    if not es_potencia:
        raise ValueError(f"q={q} no es una potencia de primo")
    if k < 1:
        raise ValueError(f"k={k} debe ser >= 1")
    if k > n:
        raise ValueError(f"k={k} debe ser <= n={n}")

    resultado["parametros"] = {"n": n, "k": k, "q": q, "p": p, "m": m}

    # 2. Puntos de evaluación y tablas del campo
    puntos = list(range(n))
    resultado["puntos_evaluacion"] = puntos
    resultado["tabla_adicion"] = tabla_adicion(q)
    resultado["tabla_multiplicacion"] = tabla_multiplicacion(q)

    # 3. Polinomios
    coeficientes = generar_coeficientes(q, k)
    resultado["polinomios"] = [formatear_polinomio(c) for c in coeficientes]
    resultado["total_polinomios"] = len(coeficientes)

    # 4. Palabras código
    palabras = generar_palabras_codigo(puntos, coeficientes, q)
    resultado["palabras_codigo"] = palabras
    resultado["total_palabras"] = len(palabras)

    # 5. Matriz generadora
    G = matriz_generadora(puntos, k, q)
    resultado["matriz_G"] = G

    # 6. Matriz de paridad
    H = matriz_paridad(G, q)
    resultado["matriz_H"] = H

    # 7. Verificar palabras código
    if H:
        resultado["palabras_validas"] = verificar_palabras_codigo(palabras, H, q)

    # 8. Distancia mínima
    resultado["distancia_minima"] = distancia_minima(palabras)
    resultado["cota_singleton"] = n - k + 1
    resultado["es_rs"] = n <= q

    # 9. Verificación cruzada
    resultado["verificacion_cruzada"] = verificacion_cruzada(palabras, G, q, k)

    return resultado


# ── main ──────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    res = calcular(n=5, k=3, q=5)
    print(f"RS({res['parametros']['n']},{res['parametros']['k']}) sobre GF({res['parametros']['q']})")
    print(f"Distancia mínima: {res['distancia_minima']}")
    print(f"Cota Singleton:   {res['cota_singleton']}")
    print(f"Total palabras:   {res['total_palabras']}")
    print(f"Es RS:            {res['es_rs']}")
    print(f"Verificación cruzada: {res['verificacion_cruzada']}")