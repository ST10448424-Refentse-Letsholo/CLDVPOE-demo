#!/bin/bash
websites=(
  "https://google.com"
  "https://github.com"
  "https://thissitedoesnotexist12345.com"
)

for site in "${websites[@]}"; do
  if curl -s --head --max-time 5 "$site" > /dev/null; then
    echo "$site is reachable"
  else
    echo "$site is NOT reachable"
  fi
done