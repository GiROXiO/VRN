from fastapi import FastAPI
from Core.Point1.Reed_Solomon import calcular
from Core.Point2.LinearCode import calcular_punto2
from Core.Point3.CyclicCode import calcular_caso

app = FastAPI()

@app.get("/punto1")
def punto1(n: int, k: int, q: int):
    try:
        resultado = calcular(n, k, q)
        return resultado
    except ValueError as e:
        return {"error": str(e)}

@app.get("/punto2")
def punto2():
    try:
        resultado = calcular_punto2()
        return resultado
    except Exception as e:
        return {"error": str(e)}

@app.get("/punto3")
def punto3(caso: int = 1):
    try:
        resultado = calcular_caso(caso)
        return resultado
    except ValueError as e:
        return {"error": str(e)}