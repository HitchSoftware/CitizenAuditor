# CitizenAuditor Setup — Windows 11 + WSL2 (Ubuntu 22.04)

This guide gets you from zero → ingesting the Orange County budget PDF into Pinecone.

---

## 1) WSL2 + Ubuntu

```powershell
wsl --install -d Ubuntu-22.04
````

Launch Ubuntu from Start Menu and update:

```bash
sudo apt update && sudo apt upgrade -y
```

(Recommended) Enable systemd so Docker can run as a service:

```bash
printf "[boot]\nsystemd=true\n" | sudo tee /etc/wsl.conf
# Close all WSL windows, then in PowerShell:
wsl --shutdown
# Re-open Ubuntu
```

---

## 2) .NET 8 SDK

Add Microsoft package feed (Ubuntu 22.04):

```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/msprod.deb
sudo dpkg -i /tmp/msprod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0
dotnet --info
```

---

## 3) Docker (inside WSL)

```bash
sudo apt install -y docker.io
sudo usermod -aG docker $USER
sudo systemctl enable --now docker
# Re-login or `newgrp docker` then:
docker info
```

If `docker info` fails, confirm systemd is enabled (see step 1).

---

## 4) Minikube (optional for Week 3)

```bash
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube
minikube start --driver=docker
minikube kubectl -- get nodes
```

---

## 5) Clone repo

```bash
cd /mnt/c/Users/drewj/src
git clone https://github.com/<your-username>/citizen-auditor
cd citizen-auditor
```

---

## 6) API keys

Add to `~/.bashrc` (replace with your values):

```bash
echo 'export OPENAI_API_KEY=sk-xxxx' >> ~/.bashrc
echo 'export PINECONE_API_KEY=pcn-xxxx' >> ~/.bashrc
source ~/.bashrc
```

**Pinecone index:** Create one named `orange-budgets` with **1536** dimensions (for `text-embedding-3-small`).

---

## 7) Ingest

```bash
cd scripts/CitizenAuditor.Ingest
dotnet restore
dotnet run
```

**Expected:**

```
Chunked 5063 pieces.
✅ All chunks ingested into Pinecone.
```

---

## Troubleshooting

* **SKEXP0010 warning** (Semantic Kernel preview API):
  Add to your `.csproj`:

  ```xml
  <PropertyGroup>
    <NoWarn>$(NoWarn);SKEXP0010</NoWarn>
  </PropertyGroup>
  ```

  or update code to `AddOpenAIEmbeddingGenerator` when stable.

* **Push fails**: `git remote -v`, then `git remote set-url origin https://github.com/<user>/citizen-auditor.git`

* **Docker perms**: `newgrp docker` after adding user to group; ensure systemd is on.

* **WSL paths**: From WSL, the repo at `C:\Users\drewj\src\CitizenAuditor` is `/mnt/c/Users/drewj/src/CitizenAuditor`.

