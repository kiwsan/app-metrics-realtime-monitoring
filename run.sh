 
#!/bin/bash

sudo mkdir -p /volumes
docker-compose up -d
#sudo chown -R 777 volumes
sudo chown -R 472:472 volumes
sudo chown -R 472:472 volumes/influxdb

echo "Grafana: http://127.0.0.1:3001 - admin/admin"

echo
echo "Current database list"
curl -G http://localhost:8086/query?pretty=true --data-urlencode "db=glances" --data-urlencode "q=SHOW DATABASES"

echo
echo "Create a new database ?"
echo "curl -XPOST 'http://localhost:8086/query' --data-urlencode 'q=CREATE DATABASE mydb'"