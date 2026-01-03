# Deployment Guide

Production deployment guide for Network Optimizer.

## Deployment Options

| Option | Best For | Guide |
|--------|----------|-------|
| Linux + Docker | Self-built servers, VMs, cloud | [Below](#1-linux-docker-recommended) |
| NAS + Docker | Synology, QNAP, Unraid | [NAS Deployment](#2-nas-deployment-docker) |
| macOS Native | Mac servers, multi-gigabit speed testing | [Native Guide](NATIVE-DEPLOYMENT.md#macos-deployment) |
| Linux Native | Maximum performance, no Docker | [Native Guide](NATIVE-DEPLOYMENT.md#linux-deployment) |
| Windows | Windows servers | [Native Guide](NATIVE-DEPLOYMENT.md#windows-deployment) |

---

### 1. Linux + Docker (Recommended)

Deploy on any Linux server using Docker Compose. This is the recommended approach for self-built NAS, home servers, VMs, and cloud instances.

**Requirements:**
- Docker 20.10+ and Docker Compose 2.0+
- 2GB RAM minimum (4GB recommended)
- 10GB disk space
- Ubuntu 20.04+, Debian 11+, RHEL/CentOS 8+, or compatible

#### Quick Start

```bash
# Install Docker (if not already installed)
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# Log out and back in for group changes

# Clone repository
git clone https://github.com/Ozark-Connect/NetworkOptimizer.git
cd network-optimizer/docker

# Configure environment (optional - defaults work out of the box)
cp .env.example .env
nano .env  # Set timezone and other options

# Start with host networking (recommended for Linux)
docker compose up -d

# Check logs for the auto-generated admin password
docker compose logs network-optimizer | grep -A5 "FIRST-RUN"

# Verify health
docker compose ps
curl http://localhost:8042/api/health
```

Access at: **http://your-server:8042**

#### Network Mode Options

**Host Networking (Recommended for Linux):**
```yaml
# docker-compose.yml uses network_mode: host by default
# This provides best performance and accurate IP detection
```

**Bridge Networking (if host mode unavailable):**
```bash
# Use docker-compose.macos.yml which uses port mapping
# IMPORTANT: Set HOST_IP in .env to your server's IP for accurate path analysis
docker compose -f docker-compose.macos.yml up -d
```

#### Service Management

```bash
# View logs
docker compose logs -f

# Restart
docker compose restart

# Stop
docker compose down

# Update to latest
docker compose pull
docker compose up -d

# Full rebuild (after Dockerfile changes)
docker compose build --no-cache
docker compose up -d
```

#### Systemd Integration (Auto-Start on Boot)

```bash
# Enable Docker to start on boot
sudo systemctl enable docker

# Docker Compose containers with restart: unless-stopped will auto-start
```

Or create a dedicated systemd service:

```bash
sudo cat > /etc/systemd/system/network-optimizer.service << 'EOF'
[Unit]
Description=Network Optimizer
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/network-optimizer/docker
ExecStart=/usr/bin/docker compose up -d
ExecStop=/usr/bin/docker compose down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable network-optimizer
```

---

### 2. NAS Deployment (Docker)

For commercial NAS devices with container support.

#### Synology NAS

1. Install Container Manager from Package Center
2. Clone or upload the repository to `/docker/network-optimizer`
3. Copy `.env.example` to `.env` and configure
4. Create project in Container Manager pointing to docker-compose.yml
5. Start containers

**Note:** If using bridge networking, set `HOST_IP` in `.env` to your NAS IP address.

#### QNAP NAS

1. Install Container Station
2. Create shared folders
3. Import `docker-compose.yml`
4. Configure environment variables
5. Deploy stack

#### Unraid

1. Install Community Applications plugin
2. Search for "Network Optimizer" (when published)
3. Or use manual Docker Compose deployment

### 4. Native Deployment (No Docker)

For maximum network performance or systems without Docker, run natively on the host.

**Best for:**
- macOS systems (avoids Docker Desktop's ~1.8 Gbps network throughput limitation)
- Systems where Docker overhead is undesirable
- Dedicated appliances

**Supported Platforms:**
- macOS 11+ (Intel or Apple Silicon)
- Linux (Ubuntu 20.04+, Debian 11+, RHEL 8+)
- Windows Server 2019+ / Windows 10+

See [Native Deployment Guide](NATIVE-DEPLOYMENT.md) for detailed instructions.

## Pre-Deployment Checklist

- [ ] Docker and Docker Compose installed
- [ ] Sufficient disk space (10GB minimum)
- [ ] Network access to UniFi Controller
- [ ] Firewall rules configured (if applicable)
- [ ] `.env` file configured with secure passwords
- [ ] SSL certificates ready (if using HTTPS)

## Installation Steps

### 1. Download Files

```bash
git clone https://github.com/Ozark-Connect/NetworkOptimizer.git
cd NetworkOptimizer/docker
```

### 2. Configure Environment

```bash
# Copy template
cp .env.example .env

# Edit with your settings
nano .env
```

**Recommended changes:**
```env
# Set your timezone
TZ=America/Chicago
```

**Admin Password:**

On first run, an auto-generated password is displayed in the logs. After logging in,
go to **Settings > Admin Password** to set your own password (recommended).

Password precedence: Database (Settings UI) > `APP_PASSWORD` env var > Auto-generated

Optionally, set `APP_PASSWORD` in `.env` if you want to configure a password before first login.

### 3. Deploy Stack

```bash
docker-compose up -d
```

### 4. Verify Deployment

```bash
# Check service health
docker-compose ps

# View logs
docker-compose logs -f

# Test health endpoint
curl http://localhost:8042/api/health
```

Expected output:
```
NAME                          STATUS
network-optimizer             Up (healthy)
```

### 5. Access Web UI

- Web UI: http://your-server:8042

## Production Configuration

### HTTPS with Reverse Proxy

Use nginx, Caddy, or Traefik for SSL termination.

#### Nginx Example

```nginx
# /etc/nginx/sites-available/network-optimizer
server {
    listen 80;
    server_name network-optimizer.example.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name network-optimizer.example.com;

    ssl_certificate /etc/letsencrypt/live/network-optimizer.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/network-optimizer.example.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Blazor Web UI
    location / {
        proxy_pass http://localhost:8042;
        proxy_http_version 1.1;

        # WebSocket support for Blazor
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Timeouts for long-running operations
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}
```

Enable and restart:
```bash
sudo ln -s /etc/nginx/sites-available/network-optimizer /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

#### Caddy Example (Automatic HTTPS)

```caddy
# /etc/caddy/Caddyfile
network-optimizer.example.com {
    reverse_proxy localhost:8042
}
```

Restart Caddy:
```bash
sudo systemctl reload caddy
```

### Firewall Configuration

#### UFW (Ubuntu/Debian)

```bash
# Allow SSH
sudo ufw allow 22/tcp

# Allow HTTP/HTTPS (if using reverse proxy)
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Or allow direct access to the web UI
sudo ufw allow 8042/tcp  # Web UI

sudo ufw enable
```

#### firewalld (RHEL/CentOS)

```bash
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --permanent --add-port=8042/tcp
sudo firewall-cmd --reload
```

### Backup Strategy

#### Automated Backups

Create backup script:
```bash
#!/bin/bash
# /usr/local/bin/backup-network-optimizer.sh

BACKUP_DIR=/backups/network-optimizer
DATE=$(date +%Y%m%d-%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup SQLite data and configuration
tar czf $BACKUP_DIR/data-$DATE.tar.gz -C /path/to/docker data/

# Cleanup old backups (keep last 7 days)
find $BACKUP_DIR -type f -mtime +7 -delete

echo "Backup completed: $DATE"
```

Add to crontab:
```bash
# Daily backup at 2 AM
0 2 * * * /usr/local/bin/backup-network-optimizer.sh >> /var/log/network-optimizer-backup.log 2>&1
```

#### Restore from Backup

```bash
# Stop services
docker-compose down

# Restore data
tar xzf /backups/network-optimizer/data-20240101-020000.tar.gz -C /path/to/docker/

# Start services
docker-compose up -d
```

### Monitoring and Alerting

#### System Monitoring

Use Docker healthchecks:
```bash
# Check all services
watch docker-compose ps

# Monitor resource usage
docker stats
```

#### Log Monitoring

Centralized logging with rsyslog or similar:
```yaml
# docker-compose.yml addition
logging:
  driver: syslog
  options:
    syslog-address: "udp://your-syslog-server:514"
    tag: "network-optimizer"
```

#### Uptime Monitoring

Use external monitoring:
- UptimeRobot
- Healthchecks.io
- Self-hosted Uptime Kuma

Configure health check endpoint:
```bash
# Monitor this endpoint
http://your-server:8042/api/health
```

### Resource Limits

Add resource constraints for production:

```yaml
# docker-compose.override.yml
services:
  network-optimizer:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '1.0'
          memory: 1G
    restart: always
```

Apply with:
```bash
docker-compose up -d
```

## High Availability Setup

For mission-critical deployments:

### Option 1: Active-Passive with Shared Storage

1. Two servers with shared NFS storage
2. Primary runs all services
3. Standby monitors primary health
4. Automatic failover on failure

### Option 2: Load Balanced

1. Multiple instances behind load balancer
2. Shared InfluxDB cluster
3. Session affinity for Blazor SignalR

## Upgrade Procedure

### Minor Version Updates

```bash
# 1. Backup current state
./backup-network-optimizer.sh

# 2. Pull latest images
docker-compose pull

# 3. Recreate containers
docker-compose up -d

# 4. Verify
docker-compose ps
docker-compose logs -f
```

### Major Version Updates

```bash
# 1. Read release notes
# 2. Backup everything
# 3. Test in staging environment
# 4. Schedule maintenance window
# 5. Update production

# Stop services
docker-compose down

# Pull new version
docker-compose pull

# Update configuration if needed
# (check release notes for breaking changes)

# Start services
docker-compose up -d

# Monitor logs
docker-compose logs -f

# Verify functionality
```

## Troubleshooting

### Service Won't Start

```bash
# Check logs
docker-compose logs <service>

# Check disk space
df -h

# Check permissions
ls -la data/ logs/

# Verify .env file
cat .env
```

### High Memory Usage

```bash
# Check resource usage
docker stats

# Restart the service
docker-compose restart network-optimizer
```

### Network Issues

```bash
# Check network
docker network ls
docker network inspect network-optimizer_default

# Recreate network
docker-compose down
docker-compose up -d
```

### Data Corruption

```bash
# Stop services
docker-compose down

# Restore from backup
tar xzf backup.tar.gz

# Start services
docker-compose up -d
```

## Security Hardening

### 1. Non-Root User

Run containers as non-root:
```yaml
services:
  network-optimizer:
    user: "1000:1000"
```

### 2. Read-Only Root Filesystem

```yaml
services:
  network-optimizer:
    read_only: true
    tmpfs:
      - /tmp
```

### 3. Drop Capabilities

```yaml
services:
  network-optimizer:
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
```

### 4. Security Scanning

```bash
# Scan images for vulnerabilities
docker scan ozark-connect/network-optimizer:latest
```

### 5. Secrets Management

Use Docker secrets or environment file:
```bash
# Store secrets securely
chmod 600 .env
```

## Performance Optimization

### 1. SSD Storage

Use SSD storage for the data directory for best performance with SQLite database.

### 2. Increase File Descriptors

```bash
# /etc/sysctl.conf
fs.file-max = 65535

# Apply
sudo sysctl -p
```

### 3. Docker Daemon Optimization

```json
// /etc/docker/daemon.json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "storage-driver": "overlay2"
}
```

## Support and Maintenance

### Getting Help

- Documentation: See `docs/` folder in repository
- GitHub Issues: https://github.com/Ozark-Connect/NetworkOptimizer/issues
- Email: tj@ozarkconnect.net

### Maintenance Windows

Recommended schedule:
- **Daily:** Automated backups
- **Weekly:** Review logs and metrics
- **Monthly:** Check for updates, security patches
- **Quarterly:** Full system audit, restore test

### Logs to Monitor

```bash
# Application logs
docker-compose logs -f network-optimizer

# System logs
journalctl -u docker -f

# Disk usage
du -sh data/ logs/
```

## Compliance and Auditing

### Data Retention

Configure according to your requirements:
- Application data: Stored in SQLite database in `data/` directory
- Logs: Docker logging driver settings
- Backups: Backup script retention period

### Access Logging

Enable detailed access logs:
```yaml
environment:
  - Logging__LogLevel__Microsoft.AspNetCore=Information
```

### Audit Trail

Network Optimizer logs all configuration changes and audits.

## Next Steps

After deployment:
1. Access web UI and complete initial setup
2. Connect to UniFi Controller
3. Configure SSH access for gateway and devices
4. Run security audit
5. Configure SQM settings (if applicable)

See main documentation for feature guides.
