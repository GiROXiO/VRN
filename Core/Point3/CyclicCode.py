import numpy as np
import sympy as sp
from sympy.polys.matrices import DomainMatrix
from sympy import GF
import itertools as it

n = 7
x = sp.Symbol('x')
F2 = GF(2)

def obtener_coeficientes(poly, tamano, F):
    coefs = poly.all_coeffs()[::-1]
    lista = [F(int(c)) for c in coefs]
    while len(lista) < tamano:
        lista.append(F(0))
    return lista

def construir_G(g_poly, n, k, F):
    r = g_poly.degree()
    coefs = obtener_coeficientes(g_poly, r + 1, F)
    primera = coefs + [F(0)] * (n - len(coefs))
    filas = [primera]
    for i in range(1, k):
        fila = [F(0)] + filas[i-1][:n-1]
        filas.append(fila)
    G = DomainMatrix(filas, (k, n), F)
    return [[int(G.to_Matrix()[i, j]) for j in range(n)] for i in range(k)]

def construir_H(h_poly, n, r, F):
    coefs = [F(int(c)) for c in h_poly.all_coeffs()]
    primera = coefs + [F(0)] * (n - len(coefs))
    filas = [primera]
    for i in range(1, r):
        fila = [F(0)] + filas[i-1][:n-1]
        filas.append(fila)
    H = DomainMatrix(filas, (r, n), F)
    return [[int(H.to_Matrix()[i, j]) for j in range(n)] for i in range(r)]

def generar_codewords(G_list, k):
    G = np.array(G_list, dtype=int)
    codewords = []
    for m in it.product([0, 1], repeat=k):
        cw = (np.array(m) @ G % 2).tolist()
        cw_str = ''.join(map(str, cw))
        if cw_str not in codewords:
            codewords.append(cw_str)
    return codewords

def distancia_minima(codewords):
    d_min = float('inf')
    for i in range(len(codewords)):
        for j in range(i+1, len(codewords)):
            d = sum(c1 != c2 for c1, c2 in zip(codewords[i], codewords[j]))
            if 0 < d < d_min:
                d_min = d
    return int(d_min) if d_min != float('inf') else 0

def calcular_caso(caso):
    poly_base = sp.Poly(x**n - 1, x, domain=F2)

    casos = {
        0: sp.Poly("x + 1", x, domain=F2),
        1: sp.Poly("x**3 + x**2 + 1", x, domain=F2),
        2: sp.Poly("x**3 + x + 1", x, domain=F2),
        3: sp.Poly("x**4 + x**3 + x**2 + 1", x, domain=F2),
        4: sp.Poly("x**4 + x**2 + x + 1", x, domain=F2),
        5: sp.Poly("x**6 + x**5 + x**4 + x**3 + x**2 + x + 1", x, domain=F2),
    }

    nombres = {
        0: "CP(7,4)",
        1: "Ham₂(4)",
        2: "≈Ham₂(4)",
        3: "Sim₂(4)",
        4: "≈Sim₂(4)",
        5: "CR(7,1)",
    }

    if caso not in casos:
        raise ValueError(f"Caso {caso} no existe")

    g_poly = casos[caso]
    r = g_poly.degree()
    k = n - r

    h_poly, _ = sp.div(poly_base, g_poly, domain=F2)

    G = construir_G(g_poly, n, k, F2)
    H = construir_H(h_poly, n, r, F2)
    codewords = generar_codewords(G, k)
    d = distancia_minima(codewords)

    return {
        "caso": caso,
        "nombre": nombres[caso],
        "n": n,
        "k": k,
        "r": r,
        "polinomio_generador": str(g_poly.as_expr()),
        "polinomio_control": str(h_poly.as_expr()),
        "parametros": f"[{n},{k},{d}]",
        "matriz_G": G,
        "matriz_H": H,
        "codewords": codewords,
        "distancia_minima": d,
        "total_codewords": len(codewords)
    }