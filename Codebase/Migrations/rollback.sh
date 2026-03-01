#!/bin/bash
set -e 

# Đợi Postgres sẵn sàng bằng công cụ pg_isready có sẵn trong postgresql-client
echo ">>> Waiting for Postgres to be ready..."
until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER"; do
  echo ">>> Postgres is unavailable - sleeping"
  sleep 2
done

echo ">>> Postgres is up - starting migrations..."

# Di chuyển vào đúng thư mục chứa script để đảm bảo đường dẫn tương đối tới ./Rollback hoạt động
cd "$(dirname "$0")"

SQL_DIR="./Rollback"

# Kiểm tra thư mục có tồn tại không
if [ ! -d "$SQL_DIR" ]; then
  echo "Error: Directory $SQL_DIR not found."
  exit 1
fi

FILES=$(ls $SQL_DIR/*.sql | sort -r)

export $(grep -v '^#' ../../.env | xargs)

SQL_DIR="./Rollback"

echo ">>> Rolling back..."
for f in $FILES
do
  echo ">>> Applying: $f"
  # Sử dụng các biến env được truyền từ docker-compose
  PGPASSWORD=$POSTGRES_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $POSTGRES_USER -d $POSTGRES_DB -f $f
done
echo ">>> Rollback completed successfully."