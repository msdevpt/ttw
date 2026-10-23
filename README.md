# ttw

`ttw` is a command line tool for TomTom sports watches. It talks to the watch
over USB and does two things:

* **update** downloads the GPS QuickFix (ephemeris) data and writes it
  to the watch;
* **activities** copies the recorded activities of a date range to
  files.

It is a port of the `tui` package of the Java project
[TomTomWatch](https://github.com/scubajorgen/TomTomWatch) and keeps its
behaviour: the same USB wire protocol, the same TTBIN parsing and the same
GPX and TCX output.

## Requirements

* A TomTom Multisport, Runner, Spark Music, Spark Cardio, Runner Music,
  Runner Cardio or Adventurer (USB product ids `1390:7474`, `1390:7475` and
  `1390:7477`).
* .NET 10 to build and run from source.

On Linux the watch is accessed through **libusb**:

```sh
pacman -S libusb            # Arch
sudo apt install libusb-1.0-0 # Debian and Ubuntu
```

The native library is shipped with the published binaries on Windows and
macOS, but not on Linux.

To be able to open the watch, the device node has to be writable. Install this
udev rule:

```
# /etc/udev/rules.d/99-tomtomwatch.rules
SUBSYSTEM=="usb", ATTRS{idVendor}=="1390", ATTRS{idProduct}=="7474|7475|7477", MODE="0660", GROUP="plugdev"
```

Then reload the rules and add your user to the `plugdev` group:

```sh
sudo udevadm control --reload-rules && sudo udevadm trigger
sudo usermod -aG plugdev "$USER"
```

Log out and back in to pick up the new group membership. No `sudo` is needed
to run `ttw` afterwards.

## Build and test

```sh
dotnet build
dotnet test
```

## Usage

```
ttw [command] [options]
```

### Global options

| Option | Description |
| --- | --- |
| `--verbose` | Show the technical detail of a failure (type, message and the URL that was being fetched) and write the logs at the debug level. Without it, a failure is reported with a message that names the service or the device that failed, without stating a cause. |

The flag can be given before or after the command:

```sh
ttw --verbose update
ttw update --verbose
```

### `ttw update`

Downloads the ephemeris data for the next three days and writes it to the
watch. The data is read back from the watch and compared byte for byte before
the GPS processor is reset.

```sh
ttw update
```

### `ttw activities`

Writes every activity that **started** in the given date range to its own
file. Both ends of the range are inclusive and the start date of an activity
is its start time in watch local time.

```sh
ttw activities --from 2025-01-01 --to 2025-01-31
```

| Option | Description |
| --- | --- |
| `--from <date>` | First day of the period, as `yyyy-MM-dd`. Required. |
| `--to <date>` | Last day of the period, as `yyyy-MM-dd`. Required. |
| `-o`, `--output <dir>` | Directory the activity files are written to. Defaults to the current directory. The directory is created when it does not exist. |
| `--format <format>` | `gpx` (default), `tcx`, `ttbin` or `csv`. |

The files are named after the start of the activity, so several activities on
the same day never collide:

```
2025-01-01_07-15-00_Running.gpx
```

| Format | Content |
| --- | --- |
| `gpx` | GPX 1.1 with the Garmin track point extensions. |
| `tcx` | Garmin Training Center XML. |
| `ttbin` | The raw file as it is stored on the watch. |
| `csv` | The track as comma separated values. |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The command succeeded. |
| `1` | The command failed. The reason is written to standard error. |

Errors go to standard error, all other output goes to standard output.

## Publishing

```sh
./scripts/publish.sh
```

This publishes self-contained binaries into `dist/win-x64`, `dist/linux-x64`
and `dist/osx-arm64`. The binary is called `ttw` (`ttw.exe` on Windows) and
bundles the .NET runtime.

## Layout

| Path | Contents |
| --- | --- |
| `src/Ttw.Core` | The TTBIN model, the reader and the export writers. No I/O. |
| `src/Ttw.Services` | The USB transport, the HTTP download, the extraction and the QuickFix update. |
| `src/Ttw.Cli` | The command line interface. |
| `tests/Ttw.Tests` | The unit tests, including the comparison with the reference files of the Java implementation. |
