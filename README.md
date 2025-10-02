# CitizenAuditor

> .NET RAG pipeline that ingests Orange County budget PDFs and flags DOGE-relevant insights (e.g., potential DEI overspend). Runs locally (Windows 11 + WSL2 + Minikube).

**Why:** Help watchdogs like @DOGEFla and citizens quickly surface anomalies, narratives, and spend hotspots in county budgets.

**Status (Week 1 / Sprint 0):**
- ✅ 1.1 Dev env
- ✅ 1.2 Repo scaffold
- ✅ 1.3 Domain prompt + artifact pull
- ✅ 1.4 Ingest → Pinecone (5,063 chunks from `OrangeBudget2025.pdf`)
- 🟨 1.5 Doc & Share (this PR)

**AI velocity:** Built **~6.7× faster with AI** (≈1.35 hours vs. ~9 hours for Week 1).

---

## Quickstart

```bash
# WSL2 (Ubuntu 22.04)
git clone https://github.com/<your-username>/citizen-auditor
cd citizen-auditor

# Set keys (or add to ~/.bashrc)
export OPENAI_API_KEY=sk-...
export PINECONE_API_KEY=pcn-...

# Ingest sample PDF (uses text-embedding-3-small → 1536 dims)
cd scripts/CitizenAuditor.Ingest
dotnet run
````

**Expected output (example):**

```
Chunked 5063 pieces.
✅ All chunks ingested into Pinecone.
Top match: ... "public safety is ~$1.2B" ...
```

---

## Architecture (MVP)

* **Ingest:** `PdfPig` → chunk → OpenAI embeddings → Pinecone index (`orange-budgets`)
* **Query:** Embed question → Pinecone top-k → (Week 2) analysis pipeline (variance, DEI flags)
* **UI:** (Week 3) Blazor + Minikube

---

## Roadmap

* **Week 2:** Clean/enrich, anomaly detection, local test
* **Week 3:** Blazor UI, Minikube deploy, PDF report for DOGE (JP)
* ⭐ Star to join the open-core mission and follow progress.

---

## Contributing

PRs welcome. Please keep issues focused and label with `week-2`, `week-3`, or `good-first-issue`.

## License

MIT © CitizenAuditor contributors



