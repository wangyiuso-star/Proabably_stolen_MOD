import json
import sys

# Usage: python update-mod-config.py <payload.json>
# Payload: { save, ids, listType, records }
# Preserves ES3 "__type" keys and unrelated mods; only touches modOrder/modTrust.
with open(sys.argv[1], "r", encoding="utf-8-sig") as handle:
    payload = json.load(handle)

save_path = payload["save"]
ids = payload["ids"]
list_type = payload["listType"]
new_records = payload["records"]

with open(save_path, "r", encoding="utf-8-sig") as handle:
    save = json.load(handle)

order = []
existing_order = save.get("modOrder")
if isinstance(existing_order, dict):
    value = existing_order.get("value")
    if isinstance(value, list):
        order = [item for item in value if isinstance(item, str)]
for mod_id in ids:
    if mod_id not in order:
        order.append(mod_id)
save["modOrder"] = {"__type": list_type, "value": order}

existing_trust = save.get("modTrust")
trust_store = {"records": [], "stopped": []}
if isinstance(existing_trust, dict):
    raw = existing_trust.get("value")
    if isinstance(raw, str) and raw.strip():
        try:
            parsed = json.loads(raw)
            if isinstance(parsed, dict):
                trust_store = parsed
        except Exception:
            trust_store = {"records": [], "stopped": []}
if not isinstance(trust_store.get("records"), list):
    trust_store["records"] = []
if not isinstance(trust_store.get("stopped"), list):
    trust_store["stopped"] = []

wanted = set(ids)
trust_store["records"] = [
    record for record in trust_store["records"]
    if not (isinstance(record, dict) and record.get("id") in wanted)
]
trust_store["records"].extend(new_records)
save["modTrust"] = {
    "__type": "string",
    "value": json.dumps(trust_store, ensure_ascii=False, separators=(",", ":")),
}

with open(save_path, "w", encoding="utf-8", newline="") as handle:
    json.dump(save, handle, ensure_ascii=False, separators=(",", ":"))

print("updated " + save_path)
