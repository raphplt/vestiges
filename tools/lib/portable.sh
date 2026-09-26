# Fonctions communes aux scripts de tools/, pour qu'ils tournent à l'identique sous Linux et macOS.
# À sourcer : source "$(dirname "$0")/lib/portable.sh"

# Chemin absolu, même si le dossier n'existe pas encore (`realpath -m` n'existe pas sous macOS).
abs_path() {
    python3 -c 'import os, sys; print(os.path.abspath(sys.argv[1]))' "$1"
}

# `timeout` de GNU coreutils, absent de macOS : repli sur gtimeout (Homebrew) puis sur perl, livré avec le système.
run_timeout() {
    local seconds=$1
    shift
    if command -v timeout >/dev/null 2>&1; then
        timeout "$seconds" "$@"
    elif command -v gtimeout >/dev/null 2>&1; then
        gtimeout "$seconds" "$@"
    else
        perl -e '
            my $seconds = shift;
            my $pid = fork() // die "fork: $!";
            if ($pid == 0) { exec @ARGV or die "exec: $!"; }
            $SIG{ALRM} = sub { kill "TERM", $pid; sleep 2; kill "KILL", $pid; exit 124; };
            alarm $seconds;
            waitpid($pid, 0);
            exit(($? & 127) ? 128 + ($? & 127) : $? >> 8);
        ' "$seconds" "$@"
    fi
}

# Charge moyenne sur une minute.
load_average() {
    if [[ -r /proc/loadavg ]]; then
        cut -d' ' -f1 /proc/loadavg
    else
        sysctl -n vm.loadavg | awk '{ print $2 }'
    fi
}

# Isole le profil Godot (sauvegardes, réglages) dans un dossier temporaire.
# Linux suit XDG ; macOS l'ignore et écrit sous ~/Library, d'où la redirection de HOME.
# Les caches .NET restent ceux de l'utilisateur pour ne pas tout retélécharger.
isolate_godot_profile() {
    local dir=$1
    export XDG_DATA_HOME="$dir/data" XDG_CONFIG_HOME="$dir/config" XDG_CACHE_HOME="$dir/cache"
    if [[ $(uname) == Darwin ]]; then
        export NUGET_PACKAGES="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
        export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$HOME}"
        export HOME="$dir/home"
        # Godot y crée son cache de shaders avant d'avoir créé le dossier user:// lui-même.
        mkdir -p "$HOME/Library/Application Support/Godot/app_userdata/Vestiges"
    fi
}

# Dossier user:// de Vestiges dans le profil courant.
vestiges_user_dir() {
    if [[ $(uname) == Darwin ]]; then
        echo "$HOME/Library/Application Support/Godot/app_userdata/Vestiges"
    else
        echo "${XDG_DATA_HOME:-$HOME/.local/share}/godot/app_userdata/Vestiges"
    fi
}

# Écran des fenêtres de test : jamais l'écran intégré du Mac quand un écran externe est branché
# (Godot numérote d'abord l'écran principal). VESTIGES_SCREEN=<n> impose un écran.
godot_screen_args() {
    if [[ -n "${VESTIGES_SCREEN:-}" ]]; then
        echo "--screen $VESTIGES_SCREEN"
    elif [[ $(uname) == Darwin ]] && (( $(system_profiler SPDisplaysDataType 2>/dev/null | grep -c "Online: Yes") > 1 )); then
        echo "--screen 1"
    fi
}
