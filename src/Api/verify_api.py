"""Перевірка запущеного API: python3 src/Api/verify_api.py [base-url]."""
import copy
import json
import sys
import urllib.error
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080"


def request(method, path, expected, body=None, raw=None):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(BASE + path, data=data, method=method,
                                 headers={"Content-Type": "application/json"})
    try:
        response = urllib.request.urlopen(req, timeout=10)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        payload = response.read()
        assert response.status == expected, (method, path, response.status, payload)
        result = json.loads(payload) if payload else None
        if expected >= 400:
            assert "application/problem+json" in response.headers.get("Content-Type", "")
            assert result["status"] == expected
            assert not any(word in payload.decode() for word in ("Exception", "StackTrace", "/Users/"))
        print(f"OK {method} {path}: {expected}")
        return result, response.headers


body = {"customer": "C-001", "lines": [
    {"productId": "P-001", "name": "Кабель", "price": 125.5, "quantity": 2}]}
created, headers = request("POST", "/api/orders", 201, body)
location = headers["Location"]
try:
    found, _ = request("GET", location, 200)
    assert found == created and found["total"] == 251
    rows, _ = request("GET", "/api/orders", 200)
    assert any(row["id"] == created["id"] for row in rows)
    total, _ = request("GET", location + "/total", 200)
    assert total["total"] == 251
    changed = copy.deepcopy(body)
    changed["customer"] = "C-002"
    changed["lines"][0]["quantity"] = 3
    updated, _ = request("PUT", location, 200, changed)
    assert updated["createdAt"] == created["createdAt"] and updated["total"] == 376.5
    invalid, _ = request("PUT", location, 400, {"customer": "", "lines": []})
    assert set(invalid["errors"]) == {"customer", "lines"}
    unchanged, _ = request("GET", location, 200)
    assert unchanged == updated
    for bad in ({}, {"customer": "C", "lines": None}, {"customer": "C", "lines": [None]},
                {"customer": "C", "lines": [{"productId": "", "name": "", "price": -1, "quantity": 0}]}):
        result, _ = request("POST", "/api/orders", 400, bad)
        assert result["errors"]
    for raw in (b"", b"null", b"{", b'{"customer":"C","lines":[{"quantity":"abc"}]}'):
        request("POST", "/api/orders", 400, raw=raw)
    request("PUT", "/api/orders/missing", 404, body)
    request("GET", "/api/orders/missing/total", 404)
finally:
    request("DELETE", location, 204)
request("GET", location, 404)
request("DELETE", location, 404)
print("CRUD та перевірки помилок пройдено.")
