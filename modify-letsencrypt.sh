#!/bin/bash

if ! [ -x "$(command -v docker-compose)" ]; then
  echo 'Error: docker-compose is not installed.' >&2
  exit 1
fi

die () {
    echo >&2 "$@"
    exit 1
}

[ "$#" -ge 2 ] || die "usage: modify-letsencrypt.sh [staging | production] [email] <domain1 domain2 ...>"

domains=(${@:3})
rsa_key_size=4096
email="$2" # Adding a valid address is strongly recommended
staging=0 # Set to 1 if you're testing your setup to avoid hitting request limits

# Enable staging mode if needed
if [ $staging != "0" ]; then staging_arg="--staging"; fi

domainString="";
for domain in "${domains[@]}"; 
do
  domainString="$domainString -d $domain"
done

docker-compose -f docker-compose.production.yml run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot \
    $staging_arg \
    $email_arg \
    $domainString \
    --rsa-key-size $rsa_key_size \
    --agree-tos \
    --force-renewal" certbot
echo
