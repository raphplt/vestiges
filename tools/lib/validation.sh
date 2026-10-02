# Validation commune aux lanceurs : codes stricts, délais, import terminé et résultat explicite.
source tools/lib/portable.sh

validation_entry() {
    if python3 tools/with_validation_lock.py --check; then return 0; fi
    local exit_code=0
    python3 tools/with_validation_lock.py bash "$@" || exit_code=$?
    exit "$exit_code"
}

validation_cleanup() {
    local exit_code=$? directory=$1
    if [[ $exit_code -ne 0 || ${VALIDATION_KEEP_LOGS:-0} == 1 ]]; then
        echo "Journaux conservés : $directory" >&2
    else
        rm -rf "$directory"
    fi
}

validation_run() {
    local seconds=$1 log=$2 pattern=$3 exit_code=0
    shift 3
    mkdir -p "$(dirname "$log")" || return 1
    run_timeout "$seconds" "$@" >"$log" 2>&1 || exit_code=$?
    if [[ $exit_code -ne 0 ]]; then
        echo "Commande en échec (code $exit_code) : $log" >&2
        tail -35 "$log" >&2
        return 1
    fi
    python3 tools/check_validation_log.py "$log" "$pattern"
}

validation_prepare() {
    local directory=$1
    mkdir -p "$directory" || return 1
    if [[ ${VESTIGES_VALIDATION_PREPARED_ROOT:-} == "$(pwd -P)" ]] && python3 tools/with_validation_lock.py --check; then
        echo "Build et import partagés avec le lanceur global."
        return 0
    fi
    validation_run "${VALIDATION_BUILD_TIMEOUT:-180}" "$directory/build.log" '0 (?:Warning|Avertissement)\(s\)' \
        dotnet build --nologo -warnaserror || return 1
    validation_run "${VALIDATION_IMPORT_TIMEOUT:-120}" "$directory/import.log" '^\[ DONE \] first_scan_filesystem$' \
        "${GODOT_BIN:-godot-mono}" --headless --editor --import --path . || return 1
}
