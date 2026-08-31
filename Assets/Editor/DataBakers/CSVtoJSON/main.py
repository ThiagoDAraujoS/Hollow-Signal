import csv
import json
import os
from pathlib import Path
import argparse
import sys


class JsonBuilder:
    def build_masteries_map(self, path):
        masteries = {}

        with open(path, 'r', encoding='utf-8') as file:
            reader = csv.reader(file)
            for row in reader:
                if not row:
                    continue

                name = row[0].strip()
                description = row[1].strip() if len(row) > 1 else ""
                raw_requirements = row[2].strip() if len(row) > 2 else ""
                req_elements = [req.strip() for req in raw_requirements.split(';') if req.strip()]
                requirements = []
                for req in req_elements:
                    parts = [part.strip() for part in req.split(':') if part.strip()]
                    if parts:
                        requirements.append(parts)
                bonuses = [skill.strip().replace(" ", "") for skill in row[3:] if skill.strip()]
                masteries[name] = {
                    "name": name,
                    "description": description,
                    "requirements": requirements,
                    "bonuses": bonuses
                }
        self.export_to_json(masteries, str(Path(path).with_suffix('.json')))
        return masteries

    def build_archetypes_map(self, path):
        archetypes = {}
        current_archetype = None
        current_skill = None

        with open(path, 'r', encoding='utf-8') as file:
            reader = csv.reader(file)
            for row in reader:
                if not any(row):
                    continue

                row += [""] * (5 - len(row))

                archetype_col = row[0].strip()
                # Clean up outer spaces, then strip ALL internal spaces!
                skill_col = row[2].strip().replace(" ", "")
                success_col = row[3].strip()
                failure_col = row[4].strip()

                if archetype_col:
                    current_archetype = archetype_col
                    current_skill = None
                    if current_archetype not in archetypes:
                        archetypes[current_archetype] = {}

                if not current_archetype:
                    continue

                if skill_col:
                    current_skill = skill_col
                    if current_skill not in archetypes[current_archetype]:
                        archetypes[current_archetype][current_skill] = {
                            "success": [],
                            "failures": []
                        }

                if not current_skill:
                    continue

                if success_col:
                    archetypes[current_archetype][current_skill]["success"].append(success_col)

                if failure_col:
                    archetypes[current_archetype][current_skill]["failures"].append(failure_col)

        self.export_to_json(archetypes, str(Path(path).with_suffix('.json')))

        return archetypes

    def export_to_json(self, data, output_path):
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)  # type: ignore


if __name__ == '__main__':
    # Set up the command line argument parser
    parser = argparse.ArgumentParser(description="Convert Bren Game CSVs to JSON.")

    # Add optional arguments for each of your file types
    parser.add_argument('--masteries', type=str, help='Path to the masteries CSV')
    parser.add_argument('--archetypes', type=str, help='Path to the archetypes CSV')
    parser.add_argument('--auto', action='store_true', help='Auto select CSV files in the script\'s folder')
    # Parse what the user typed in the console
    args = parser.parse_args()
    builder = JsonBuilder()

    # Check which arguments were provided and run the corresponding method
    processed_any = False

    if args.auto:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        auto_targets = {
            'masteries.csv': builder.build_masteries_map,
            'archetypes.csv': builder.build_archetypes_map
        }
        for filename in os.listdir(script_dir):
            if filename.lower() in auto_targets:
                full_path = os.path.join(script_dir, filename)
                auto_targets[filename.lower()](full_path)

                print(f"Success: Auto-processed -> {filename}")
                processed_any = True
    else:
        if args.masteries:
            builder.build_masteries_map(args.masteries)
            print(f"Success: Processed Masteries -> {args.masteries}")
            processed_any = True

        if args.archetypes:
            builder.build_archetypes_map(args.archetypes)
            print(f"Success: Processed Archetypes -> {args.archetypes}")
            processed_any = True

    if not processed_any:
        parser.print_help()
        sys.exit(1)
