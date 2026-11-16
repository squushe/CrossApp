"""Перевірка запущеного API: python3 src/Api/verify_api.py [base-url]."""
import copy
import json
import sys
import uuid
import urllib.parse
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


unique_customer = "verify-" + uuid.uuid4().hex
body = {"customer": unique_customer, "lines": [
    {"productId": "P-001", "name": "Кабель", "price": 125.5, "quantity": 2}]}
created, headers = request("POST", "/api/orders", 201, body)
location = headers["Location"]
try:
    found, _ = request("GET", location, 200)
    assert found == created and found["total"] == 251
    rows, _ = request("GET", "/api/orders", 200)
    assert isinstance(rows["items"], list) and rows["total"] >= 1
    search_path = "/api/orders?page=1&pageSize=1&search=" + urllib.parse.quote(unique_customer)
    page, _ = request("GET", search_path, 200)
    assert page["total"] == 1 and page["items"][0]["id"] == created["id"]
    beyond, _ = request("GET", search_path.replace("page=1&", "page=2&"), 200)
    assert beyond["total"] == 1 and beyond["items"] == []
    invalid_page, _ = request("GET", "/api/orders?page=0&pageSize=101", 400)
    assert set(invalid_page["errors"]) == {"page", "pageSize"}
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
print("CRUD, пагінація, пошук та перевірки помилок пройдено.")
