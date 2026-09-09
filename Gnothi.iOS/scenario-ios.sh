#!/usr/bin/env bash
#
# RAISES THE INSTITUTION SCENARIO ON A MAC, so the hosted-module tests can be run against a
# simulator or a device. It is the twin of Gnothi.Mobile/escenario-webview.ps1 and it does
# LESS, for two reasons that are both in our favour.
#
# 1. NO TUNNELS. The iOS Simulator shares the Mac's network stack, so 127.0.0.1 inside the
#    simulator IS the Mac's localhost. Android needs `adb reverse` for exactly this and it
#    was the single most common cause of "the module opens empty and nothing says why".
#
# 2. NO INSTALL. Xcode -- or `dotnet build -t:Run` -- puts the app on the simulator. There is
#    no adb here and nothing to approve: the universal link is declared in the entitlement.
#
# THE BUNDLE CATALOG IS NOT DUPLICATED HERE, and that is deliberate. Which project each
# module publishes, its title, its resource, its port and its declared tools are ~200 lines
# of DATA that live in Gnothi.Mobile/armar-bundle.ps1. A second copy is how the manifest
# antipattern started: two declarations, one goes stale in silence, and the suite ends up
# verifying the wrong one. So this script CALLS that one through PowerShell.
#
#   brew install --cask powershell     # if pwsh is not there
#
# WHAT IT LEAVES RUNNING: eight servers, a publisher and an institution per module.
#
#     examenes    :8095 publisher   :8096 institution
#     compras     :8101             :8102
#     cargas      :8111             :8112
#     dealership  :8092             :8093
#
# WHAT WAS MEASURED, and from Windows under Git Bash, before this ever saw a Mac: the eight
# servers come up and all eight /salud answer; the multi-word titles arrive INTACT (see the
# note on stop() -- the PowerShell twin gets this wrong); and the bundle the page advertises
# downloads (14.4 MB of compras.zip over the alta link's URL).
#
# WHAT WAS NOT: the stop path, because Git Bash has no usable pgrep and its ps shows no
# command lines. It is written against lsof, which macOS has, and it verifies itself instead
# of claiming success -- which is what the first version got wrong.

set -uo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/.." && pwd)"
mobile="$root/Gnothi.Mobile"
logs="${TMPDIR:-/tmp}/gnothi-scenario"
pidfile="$logs/pids"

# Same four institutions, same ports as the PowerShell one. Kept as a flat list and not as a
# second catalog: what is here is only what a server needs to be STARTED, never what the
# manifest declares.
modules=(
  "examenes|8095|8096|Examenes/EndPointDeEventos|Laboratorio Clinico Delfos|Mis examenes"
  "compras|8101|8102|Compras/EndPointDeEventos|Supermercado La Guaria|Mis compras"
  "cargas|8111|8112|Cargas/EndPointDeEventos|Estaciones Faro|Mis cargas"
  "dealership|8092|8093|DealershipCar/EndPointDeEventos|DealershipCar|My maintenance"
)

ports=()
for entry in "${modules[@]}"; do
  IFS='|' read -r _ pub web _ _ _ <<< "$entry"
  ports+=("$pub" "$web")
done

alive() { curl -fsS -m 2 "http://127.0.0.1:$1/salud" >/dev/null 2>&1; }

# WHICH POWERSHELL. On a Mac the only name is `pwsh`; on Windows under Git Bash the only one
# is `powershell` (5.1). Accepting both costs nothing and is what let this script be tested
# from Windows before it ever reached a Mac.
shell_ps() {
  command -v pwsh >/dev/null && { echo pwsh; return; }
  command -v powershell >/dev/null && { echo powershell; return; }
  echo ""
}

# BY PORT, NOT BY NAME PATTERN, and this took being wrong twice to get right.
#
# `dotnet run` is a LAUNCHER: it builds, then starts a SECOND process, and the one holding
# the port is the second. Worse, that second process is NOT always called the same thing --
# measured on Windows, the publishers end up as apphosts named `EventosMock` and
# `DealershipCar.EventosMock` while the institutions stay under dotnet. Any pattern is
# therefore a guess that half-works: the PowerShell twin needs TWO passes for this and still
# needed both to bring the eight down.
#
# Whoever holds the port is not a guess. lsof answers it exactly, kills nothing unrelated,
# and does not care what the process is called.
stop() {
  local failed=0 pid

  if command -v lsof >/dev/null; then
    for p in "${ports[@]}"; do
      for pid in $(lsof -ti "tcp:$p" 2>/dev/null); do
        kill "$pid" 2>/dev/null && echo "  :$p  stopped pid $pid"
      done
    done
  else
    echo "  no lsof; falling back to command-line patterns, which are less exact"
    pkill -f "EndPointDeEventos" 2>/dev/null
    pkill -f "EventosMock" 2>/dev/null
    pkill -f "PaginaDeLaInstitucion" 2>/dev/null
  fi

  # The launchers hold no port, so nothing above reaches them. These are the pids this
  # script itself started, and only those.
  if [[ -f "$pidfile" ]]; then
    while read -r pid; do [[ -n "$pid" ]] && kill "$pid" 2>/dev/null; done < "$pidfile"
    rm -f "$pidfile"
  fi

  sleep 2

  # IT VERIFIES INSTEAD OF ANNOUNCING. The first version printed "scenario stopped" without
  # checking, and on the first real run it said that while all eight ports were still up.
  for p in "${ports[@]}"; do
    if alive "$p"; then echo "  :$p STILL ANSWERING"; failed=$((failed+1)); fi
  done

  if (( failed > 0 )); then
    echo "$failed port(s) did not go down. Find who holds one with:  lsof -i tcp:8095"
    return 1
  fi
  echo "scenario stopped, eight ports down."
}

if [[ "${1:-}" == "--stop" || "${1:-}" == "-s" ]]; then stop; exit $?; fi

skip_bundles=false
[[ "${1:-}" == "--no-bundles" ]] && skip_bundles=true

command -v dotnet >/dev/null || { echo "no dotnet on PATH. Install the .NET SDK."; exit 1; }
mkdir -p "$logs"
: > "$pidfile"

echo "root    : $root"
echo "logs    : $logs"
echo

for entry in "${modules[@]}"; do
  IFS='|' read -r name pub web mock title subtitle <<< "$entry"
  echo "  $name"

  zip="$mobile/bundles/$name.zip"

  # THE BUNDLE DEPENDS ON THE PORT: the manifest carries the publisher's URL INSIDE. A bundle
  # built for one port and tested against another installs fine and brings nothing back, with
  # nothing saying why. That is why it is rebuilt and not cached by default.
  if [[ "$skip_bundles" == false || ! -f "$zip" ]]; then
    ps="$(shell_ps)"
    [[ -n "$ps" ]] || {
      echo "    no PowerShell and the bundle has to be built:  brew install --cask powershell"
      echo "    (or copy bundles/ over from the Windows machine and pass --no-bundles)"
      exit 1
    }
    echo "    building the bundle..."
    "$ps" -NoProfile -File "$mobile/armar-bundle.ps1" -Modulo "$name" \
      -Publicador "http://127.0.0.1:$pub/" >"$logs/bundle-$name.log" 2>&1 || {
        echo "    the bundle of $name could not be built -- see $logs/bundle-$name.log"
        exit 1; }
  fi

  [[ -f "$zip" ]] || { echo "    there is no bundle for $name"; exit 1; }

  if alive "$pub"; then
    echo "    :$pub  publisher already up"
  else
    dotnet run --project "$root/$mock" --urls "http://0.0.0.0:$pub" \
      >"$logs/pub-$name.log" 2>&1 &
    echo "$!" >> "$pidfile"
    echo "    :$pub  publisher started"
  fi

  # THE STANDALONE APP AND THE PAGE ARE THE SAME SERVER, on purpose: what is tested loose and
  # what is tested hosted are then the same binary. The app is published with
  # <base href="/app/"> and asks for its events at a URL RELATIVE to its own origin, so served
  # alone on another port it does not start -- it says "Ocurrio un error inesperado" and never
  # mentions static files.
  #
  # EVERY ARGUMENT IS QUOTED, and that is not cosmetic. PowerShell's Start-Process joins an
  # -ArgumentList array with spaces and adds no quotes -- verified: it hands dotnet
  # `--Titulo Supermercado La Guaria`, so the page comes up titled "Supermercado" with two
  # stray arguments. Here the four titles were measured arriving intact.
  if alive "$web"; then
    echo "    :$web  institution already up"
  else
    dotnet run --project "$root/PaginaDeLaInstitucion" --urls "http://0.0.0.0:$web" \
      --Bundle "$zip" \
      --AppSuelta "$mobile/bundles/$name/web" \
      --Publicador "http://127.0.0.1:$pub/" \
      --UrlPublica "http://127.0.0.1:$web" \
      --Titulo "$title" \
      --Subtitulo "$subtitle" \
      >"$logs/web-$name.log" 2>&1 &
    echo "$!" >> "$pidfile"
    echo "    :$web  institution started"
  fi
done

echo
echo "waiting for the eight ports (up to 90s)..."

# POLLED, NOT A FIXED SLEEP. The first run of the day compiles five server projects and takes
# far longer than a warm one, so any constant is either a wrong answer or wasted waiting.
for _ in $(seq 1 45); do
  missing=0
  for p in "${ports[@]}"; do alive "$p" || missing=$((missing+1)); done
  (( missing == 0 )) && break
  sleep 2
done

bad=0
for entry in "${modules[@]}"; do
  IFS='|' read -r name pub web _ _ _ <<< "$entry"
  for p in "$pub" "$web"; do
    if alive "$p"; then printf "  :%s ok\n" "$p"
    else printf "  :%s DOES NOT ANSWER -- see %s/pub-%s.log and %s/web-%s.log\n" \
      "$p" "$logs" "$name" "$logs" "$name"; bad=$((bad+1)); fi
  done
done

echo
if (( bad > 0 )); then
  echo "SCENARIO INCOMPLETE: $bad port(s) not answering. Do not run the script yet."
  exit 1
fi

echo "SCENARIO UP."
for entry in "${modules[@]}"; do
  IFS='|' read -r name _ web _ _ _ <<< "$entry"
  printf "  %-11s page http://127.0.0.1:%s/   standalone app http://127.0.0.1:%s/app/\n" "$name" "$web" "$web"
done

cat <<'NEXT'

To look inside, in another terminal:
  log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact

P0 answers itself now -- look for the line that says:
  GNOTHI: <module>: P0 · secureContext=… crypto.subtle=… storage=… origin=…

To bring it down:  ./scenario-ios.sh --stop
NEXT
