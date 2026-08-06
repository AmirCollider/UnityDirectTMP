# Security Policy

## What this package touches

Unity DirectTMP is an editor tool and a small runtime component. It is worth
being precise about what it does, because two of its features sound alarming
until they are described exactly.

**It reads font files.** `DirectFontFile` parses the `name`, `maxp`, `head` and
`cmap` tables of `.ttf` / `.otf` / `.ttc` files — the ones in your project, and
the ones installed on your machine when you switch the Font Catalog's
"Installed fonts" toggle on. It is a read-only parse into a bounds-checked
buffer. Nothing is executed, nothing is written back, and no font file is
copied anywhere.

**It changes the Unity Editor's UI font.** Only in memory, only for the current
session, and only across `GUIStyle` and `GUISkin` objects that Unity has
already loaded. It writes nothing into your Unity installation, edits no
built-in asset on disk, and installs no startup hook outside the package.
Quitting Unity restores everything.

**It writes exactly two kinds of thing to disk.** Preferences, via
`EditorPrefs`, keyed per project; and spooled copies of fonts you hand it as a
raw `byte[]`, under `Application.persistentDataPath/UnityDirectTMP/FontCache`,
which the Clear Cache menu item deletes.

**It makes no network requests.** There is no telemetry, no analytics, no
update check and no licence server. The only URLs in the package are the ones
behind buttons a person clicks, which open in the system browser.

## Reporting a vulnerability

Please **do not** open a public issue for a security problem.

Email **amircollider@yahoo.com** with:

- what you found, and where in the package
- how to reproduce it — a crafted font file is the most useful attachment here
- what an attacker could do with it

You will get an acknowledgement within a few days. A confirmed issue is fixed in
a patch release, credited to you unless you would rather not be.

## What counts

The font parser is the part of this package most worth attacking, because it is
the only place that reads bytes it did not write. A malformed or hostile font
file that makes `DirectFontFile.Parse` throw an unhandled exception, read out of
bounds, allocate unboundedly, or hang the Editor is a genuine bug and is in
scope — even though the parse is managed C# and cannot corrupt memory. It is
supposed to return an invalid result with a reason, every time, for every input.

Also in scope: anything that makes the Editor-font override persist past a Unity
restart, or that gets past the guard that refuses a font which cannot draw the
Editor's own interface. Both would leave somebody with an Editor they cannot
undo the change from.

Out of scope: the fact that a font file you choose to load is executed by
FreeType inside Unity's own FontEngine (that is Unity's surface, not this
package's), and anything requiring the attacker to already be able to write
arbitrary files into your project.

## Supported versions

The latest release. This is a small, free package; fixes go forward rather than
into old versions.
