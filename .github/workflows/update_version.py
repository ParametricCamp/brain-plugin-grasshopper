# - - - - BUILT-IN IMPORTS
import os, re

# - - - - GLOBALS
env = os.getenv("GITHUB_ENV")
root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
solution_file = os.path.join(root, "src", "brain_ghplugin.csproj")

# Read version from solution
data = ""
with open(solution_file, 'rt') as file:
    data = file.read()
result = re.findall(r"<Version>(.*)</Version>", data)

# Add Version info to Environment File
if result is not None:
    version = result[0]
    with open(env, "a") as file:
        file.write(f"VERSION={version}")
else:
    print("No Version Match was found")
    exit(1)
