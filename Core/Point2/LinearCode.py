import numpy as np
import sympy as sp
import itertools as it

def a_mod_q(val, q):
    num = int(val.p)
    den = int(val.q)
    return (num * pow(den, -1, q)) % q

def forma_estandar(G, q, n, k):
    M = sp.Matrix(G)
    forma_reducida, columnas_pivote = M.rref()
    forma_reducida = forma_reducida.applyfunc(lambda x: a_mod_q(x, q))
    columnas_no_pivote = [c for c in range(n) if c not in columnas_pivote]
    orden_columnas = list(columnas_pivote) + columnas_no_pivote
    G_std = forma_reducida[:, orden_columnas]
    return np.array(G_std, dtype=int).tolist(), orden_columnas

def codigo_auto_dual(G_std, q, n, k):
    G_std = np.array(G_std, dtype=int)
    P = G_std[:, k:]
    H = np.hstack([(-P.T) % q, np.eye(n - k, dtype=int)]).astype(int)
    if n != 2 * k:
        return H.tolist(), False, f"No puede ser auto-dual: n={n} no es igual a 2k={2*k}"
    filas_G = set(map(tuple, G_std.tolist()))
    filas_H = set(map(tuple, H.tolist()))
    es_dual = filas_G == filas_H
    return H.tolist(), es_dual, "El código es auto-dual" if es_dual else "El código no es auto-dual"

def obtener_codewords(G, q, k):
    G = np.array(G, dtype=int)
    codewords = []
    vistos = set()
    for m in it.product(range(q), repeat=k):
        cw = tuple((np.array(m) @ G % q).tolist())
        if cw not in vistos:
            vistos.add(cw)
            codewords.append(list(cw))
    return codewords

def codigo_equivalente(G, q):
    G = np.array(G, dtype=int)
    G_equiv = G.copy()
    G_equiv[:, [0, 1]] = G_equiv[:, [1, 0]]
    return G_equiv.tolist()

def extension(G, q):
    G = np.array(G, dtype=int)
    nueva_col = ((-G.sum(axis=1)) % q).reshape(-1, 1)
    return np.hstack([G, nueva_col]).tolist()

def perforacion(G, q, i=1):
    G = np.array(G, dtype=int)
    return np.delete(G, i, axis=1).tolist()

def reduccion(G, q, i=1):
    G = np.array(G, dtype=int)
    filas = [j for j in range(G.shape[0]) if G[j, i] == 0]
    return np.delete(G[filas, :], i, axis=1).tolist()

def calcular_punto2():
    G = [[1,1,1,0,0,0,0],[1,0,0,1,1,0,0],[1,0,0,0,0,1,1],[0,1,0,1,0,1,0]]
    q = 2
    n = len(G[0])
    k = len(G)

    G_std, orden_columnas = forma_estandar(G, q, n, k)
    H, es_dual, mensaje_dual = codigo_auto_dual(G_std, q, n, k)
    G_equiv = codigo_equivalente(G, q)
    codewords_original = obtener_codewords(G, q, k)
    codewords_equiv = obtener_codewords(G_equiv, q, k)
    G_ext = extension(G, q)
    G_perf = perforacion(G, q, 1)
    G_red = reduccion(G, q, 1)

    return {
        "matriz_G_original": G,
        "n": n,
        "k": k,
        "q": q,
        "forma_estandar": G_std,
        "orden_columnas": orden_columnas,
        "matriz_H": H,
        "es_auto_dual": es_dual,
        "mensaje_dual": mensaje_dual,
        "matriz_equivalente": G_equiv,
        "codewords_original": codewords_original,
        "codewords_equivalente": codewords_equiv,
        "matriz_extension": G_ext,
        "matriz_perforacion": G_perf,
        "matriz_reduccion": G_red
    }