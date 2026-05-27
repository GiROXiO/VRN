from fastapi import FastAPI
from Core.Point1.Reed_Solomon import calcular

app = FastAPI()

@app.get("/punto1")
def punto1(n: int, k: int, q: int):
    try:
        resultado = calcular(n, k, q)
        return resultado
    except ValueError as e:
        return {"error": str(e)}