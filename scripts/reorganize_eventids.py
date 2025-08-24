#!/usr/bin/env python3
"""
Fixed EventId Reorganization Script for SnapDog2
Handles mixed formats and multi-line positional LoggerMessage patterns
"""

import os
import re
import json
from pathlib import Path
from collections import defaultdict

# Color output
class Colors:
    BLUE = '\033[0;34m'
    GREEN = '\033[0;32m'
    YELLOW = '\033[1;33m'
    RED = '\033[0;31m'
    NC = '\033[0m'

def print_colored(text, color):
    print(f"{color}{text}{Colors.NC}")

def categorize_file(file_path, snapdog_dir):
    """Categorize file based on path patterns"""
    relative_path = str(file_path.relative_to(snapdog_dir))
    
    # Path-based categorization
    if any(x in relative_path for x in ['Audio', 'Media', 'Snapcast', 'LibVLC', 'Player', 'Sound']):
        return 'Audio'
    elif any(x in relative_path for x in ['KNX', 'Knx', 'Building', 'Automation']):
        return 'KNX'
    elif any(x in relative_path for x in ['MQTT', 'Mqtt', 'Message', 'Broker', 'Topic']):
        return 'MQTT'
    elif any(x in relative_path for x in ['Web', 'Http', 'API', 'Controller', 'Endpoint']):
        return 'Web'
    elif any(x in relative_path for x in ['Infrastructure', 'Host', 'Extension', 'Service', 'Integration']):
        return 'Infrastructure'
    elif any(x in relative_path for x in ['Performance', 'Metrics', 'Monitor', 'Stats']):
        return 'Performance'
    elif any(x in relative_path for x in ['Notification', 'Event', 'Handler', 'Publisher']):
        return 'Notifications'
    elif any(x in relative_path for x in ['Test', 'Mock', 'Fake', 'Stub']):
        return 'Testing'
    else:
        return 'Core'

def get_category_base(category):
    """Get base EventId for category"""
    bases = {
        'Core': 1000,
        'Audio': 2000,
        'KNX': 3000,
        'MQTT': 4000,
        'Web': 5000,
        'Infrastructure': 6000,
        'Performance': 7000,
        'Notifications': 8000,
        'Testing': 9000
    }
    return bases.get(category, 1000)

def standardize_loggermessage_format(content):
    """
    Convert positional LoggerMessage to named parameter format
    Handles both single-line and multi-line patterns
    """
    
    # Pattern 1: Single-line positional format
    # [LoggerMessage(eventId, LogLevel.Level, "message")]
    single_line_pattern = r'\[LoggerMessage\(\s*(\d+)\s*,\s*(LogLevel\.\w+)\s*,\s*(".*?")\s*\)\]'
    
    def replace_single_line(match):
        event_id = match.group(1)
        log_level = match.group(2)
        message = match.group(3)
        
        full_log_level = f"Microsoft.Extensions.Logging.{log_level}"
        
        return f"""[LoggerMessage(
        EventId = {event_id},
        Level = {full_log_level},
        Message = {message}
    )]"""
    
    # Pattern 2: Multi-line positional format
    # [LoggerMessage(
    #     eventId,
    #     LogLevel.Level,
    #     "message"
    # )]
    multi_line_pattern = r'\[LoggerMessage\(\s*\n\s*(\d+)\s*,\s*\n\s*(LogLevel\.\w+)\s*,\s*\n\s*(".*?")\s*\n\s*\)\]'
    
    def replace_multi_line(match):
        event_id = match.group(1)
        log_level = match.group(2)
        message = match.group(3)
        
        full_log_level = f"Microsoft.Extensions.Logging.{log_level}"
        
        return f"""[LoggerMessage(
        EventId = {event_id},
        Level = {full_log_level},
        Message = {message}
    )]"""
    
    # Apply replacements
    content = re.sub(single_line_pattern, replace_single_line, content)
    content = re.sub(multi_line_pattern, replace_multi_line, content, flags=re.MULTILINE | re.DOTALL)
    
    return content

def extract_eventids(file_path):
    """Extract EventId patterns from file (handles all formats)"""
    try:
        content = file_path.read_text(encoding='utf-8')
        
        eventids = []
        
        # Named format: EventId = number
        named_pattern = r'EventId\s*=\s*(\d+)'
        named_matches = re.findall(named_pattern, content)
        eventids.extend([int(match) for match in named_matches])
        
        # Single-line positional format: [LoggerMessage(number, ...)]
        single_positional_pattern = r'\[LoggerMessage\(\s*(\d+)\s*,'
        single_matches = re.findall(single_positional_pattern, content)
        eventids.extend([int(match) for match in single_matches])
        
        # Multi-line positional format: [LoggerMessage(\n    number,
        multi_positional_pattern = r'\[LoggerMessage\(\s*\n\s*(\d+)\s*,'
        multi_matches = re.findall(multi_positional_pattern, content, re.MULTILINE)
        eventids.extend([int(match) for match in multi_matches])
        
        return sorted(list(set(eventids)))  # Remove duplicates and sort
        
    except Exception as e:
        print_colored(f"Error reading {file_path}: {e}", Colors.RED)
        return []

def has_positional_format(content):
    """Check if file has any positional LoggerMessage patterns"""
    # Single-line positional
    single_pattern = r'\[LoggerMessage\(\s*\d+\s*,\s*LogLevel\.\w+\s*,'
    if re.search(single_pattern, content):
        return True
    
    # Multi-line positional
    multi_pattern = r'\[LoggerMessage\(\s*\n\s*\d+\s*,\s*\n\s*LogLevel\.\w+\s*,'
    if re.search(multi_pattern, content, re.MULTILINE):
        return True
    
    return False

def standardize_file_format(file_path):
    """Standardize LoggerMessage format in a file"""
    try:
        content = file_path.read_text(encoding='utf-8')
        original_content = content
        
        # Standardize the format
        standardized_content = standardize_loggermessage_format(content)
        
        # Check if changes were made
        if standardized_content != original_content:
            file_path.write_text(standardized_content, encoding='utf-8')
            return True
        
        return False
        
    except Exception as e:
        print_colored(f"Error standardizing {file_path}: {e}", Colors.RED)
        return False

def replace_eventids(file_path, replacements):
    """Replace EventIds in file (only named format after standardization)"""
    try:
        content = file_path.read_text(encoding='utf-8')
        
        for old_id, new_id in replacements.items():
            # Replace EventId = old_id with EventId = new_id
            pattern = rf'(EventId\s*=\s*){old_id}\b'
            replacement = rf'\g<1>{new_id}'
            content = re.sub(pattern, replacement, content)
        
        file_path.write_text(content, encoding='utf-8')
        return True
    except Exception as e:
        print_colored(f"Error updating EventIds in {file_path}: {e}", Colors.RED)
        return False

def main():
    print_colored("🔍 Fixed EventId Reorganization Script", Colors.BLUE)
    print_colored("=====================================", Colors.BLUE)
    
    # Setup paths
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    snapdog_dir = project_root / "SnapDog2"
    
    if not snapdog_dir.exists():
        print_colored(f"Error: SnapDog2 directory not found at {snapdog_dir}", Colors.RED)
        return 1
    
    print_colored("📁 Scanning for LoggerMessage files...", Colors.YELLOW)
    
    # Find all C# files with LoggerMessage
    cs_files = []
    for cs_file in snapdog_dir.rglob("*.cs"):
        if "obj" in str(cs_file) or "Tests" in str(cs_file):
            continue
        
        try:
            content = cs_file.read_text(encoding='utf-8')
            if "LoggerMessage" in content:
                cs_files.append(cs_file)
        except:
            continue
    
    print_colored(f"📊 Found {len(cs_files)} files with LoggerMessage", Colors.YELLOW)
    
    # Phase 1: Standardize LoggerMessage format
    print_colored("\n🔄 Phase 1: Standardizing LoggerMessage format...", Colors.BLUE)
    
    standardized_count = 0
    for file_path in sorted(cs_files):
        relative_path = file_path.relative_to(snapdog_dir)
        
        # Check if file has positional LoggerMessage patterns
        content = file_path.read_text(encoding='utf-8')
        
        if has_positional_format(content):
            print_colored(f"📄 Standardizing: {relative_path}", Colors.GREEN)
            if standardize_file_format(file_path):
                standardized_count += 1
                
                # Show what was changed
                old_eventids = extract_eventids(file_path)
                if old_eventids:
                    print(f"   Found EventIds: {old_eventids}")
    
    if standardized_count > 0:
        print_colored(f"✅ Standardized {standardized_count} files", Colors.GREEN)
    else:
        print_colored("✅ All files already use standardized format", Colors.GREEN)
    
    # Phase 2: Reorganize EventIds
    print_colored("\n🔄 Phase 2: Reorganizing EventIds...", Colors.BLUE)
    
    # Re-scan files after standardization
    file_data = {}
    category_counters = defaultdict(int)
    
    for file_path in sorted(cs_files):
        category = categorize_file(file_path, snapdog_dir)
        eventids = extract_eventids(file_path)
        
        if eventids:
            file_data[file_path] = {
                'category': category,
                'eventids': sorted(eventids),
                'file_index': category_counters[category]
            }
            category_counters[category] += 1
    
    # Generate new EventId assignments
    print_colored("🔄 Generating new EventId assignments...", Colors.YELLOW)
    
    changes = {}
    summary = []
    
    for file_path, data in file_data.items():
        category = data['category']
        old_eventids = data['eventids']
        file_index = data['file_index']
        
        base_id = get_category_base(category)
        new_base = base_id + (file_index * 100)
        
        replacements = {}
        for i, old_id in enumerate(old_eventids):
            new_id = new_base + i
            if old_id != new_id:
                replacements[old_id] = new_id
        
        if replacements:
            changes[file_path] = replacements
            relative_path = file_path.relative_to(snapdog_dir)
            summary.append(f"{relative_path} ({category}): {len(replacements)} EventId changes")
    
    # Show preview
    print_colored("📋 Preview of EventId changes:", Colors.BLUE)
    if not changes:
        print_colored("No EventId changes needed - already organized!", Colors.GREEN)
    else:
        for line in summary[:10]:  # Show first 10
            print(f"  {line}")
        
        if len(summary) > 10:
            print(f"  ... and {len(summary) - 10} more files")
        
        print(f"\nTotal: {len(changes)} files will have EventId changes")
        
        # Ask for confirmation for EventId changes
        response = input("\nApply EventId reorganization? (y/N): ").strip().lower()
        if response != 'y':
            print_colored("EventId reorganization cancelled by user", Colors.YELLOW)
            return 0
        
        # Apply EventId changes
        print_colored("✏️  Applying EventId changes...", Colors.YELLOW)
        
        success_count = 0
        mapping_file = script_dir / "eventid-mappings.txt"
        
        with open(mapping_file, 'w') as f:
            f.write("# EventId Mapping Report\n")
            f.write(f"# Generated on {os.popen('date').read().strip()}\n")
            f.write("# Format: File|Category|OldEventId|NewEventId\n\n")
            
            for file_path, replacements in changes.items():
                relative_path = file_path.relative_to(snapdog_dir)
                category = file_data[file_path]['category']
                
                print_colored(f"📄 {relative_path}", Colors.GREEN)
                
                if replace_eventids(file_path, replacements):
                    success_count += 1
                    for old_id, new_id in replacements.items():
                        print(f"   {old_id} → {new_id}")
                        f.write(f"{relative_path}|{category}|{old_id}|{new_id}\n")
                else:
                    print_colored(f"   Failed to update {relative_path}", Colors.RED)
        
        print_colored(f"\n✅ Successfully updated {success_count}/{len(changes)} files", Colors.GREEN)
        print_colored(f"📊 Mapping saved to: {mapping_file}", Colors.GREEN)
    
    # Final summary
    print_colored("\n📋 Final EventId ranges:", Colors.BLUE)
    for category in sorted(category_counters.keys()):
        count = category_counters[category]
        base = get_category_base(category)
        print(f"   {category}: {count} files ({base}-{base + 999})")
    
    print_colored("\n🧪 Test the changes:", Colors.YELLOW)
    print("   dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet")
    
    return 0

if __name__ == "__main__":
    exit(main())
