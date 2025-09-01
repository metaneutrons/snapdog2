#!/usr/bin/env python3
"""
Systematic EventId Organizer - Reassigns ALL EventIds by category and file
Guarantees no duplicates and is truly idempotent
"""

import re
from pathlib import Path
from collections import defaultdict

def print_colored(text, color):
    colors = {'red': '\033[0;31m', 'green': '\033[0;32m', 'yellow': '\033[1;33m', 'blue': '\033[0;34m', 'nc': '\033[0m'}
    print(f"{colors.get(color, '')}{text}{colors['nc']}")

def categorize_file(file_path, snapdog_dir):
    """Categorize file by path - deterministic"""
    relative_path = str(file_path.relative_to(snapdog_dir))
    
    if any(x in relative_path for x in ['Audio', 'Media', 'LibVLC', 'Player']):
        return 'Audio'
    elif any(x in relative_path for x in ['KNX', 'Knx']):
        return 'KNX'  
    elif any(x in relative_path for x in ['MQTT', 'Mqtt']):
        return 'MQTT'
    elif any(x in relative_path for x in ['Api/', 'Controller', 'Health']):
        return 'Web'
    elif any(x in relative_path for x in ['Performance', 'Metrics']):
        return 'Performance'
    elif any(x in relative_path for x in ['Notification', 'Publisher', 'Handler']):
        return 'Notifications'
    elif any(x in relative_path for x in ['Infrastructure', 'Integration', 'Service', 'Storage']):
        return 'Infrastructure'
    else:
        return 'Core'

def get_category_base(category):
    """Get base EventId for category - properly spaced ranges"""
    return {
        'Core': 1000,           # 1000-1999 (5 files = 500 IDs)
        'Audio': 2000,          # 2000-2999 (5 files = 500 IDs)  
        'KNX': 3000,            # 3000-3999 (4 files = 400 IDs)
        'MQTT': 4000,           # 4000-4999 (5 files = 500 IDs)
        'Web': 5000,            # 5000-5999 (6 files = 600 IDs)
        'Infrastructure': 6000, # 6000-7999 (20 files = 2000 IDs)
        'Performance': 8000,    # 8000-8999 (6 files = 600 IDs)
        'Notifications': 10000  # 10000-15999 (32 files = 3200 IDs)
    }[category]

def main():
    print_colored("🔄 Systematic EventId Organizer", 'blue')
    
    snapdog_dir = Path(__file__).parent.parent / "SnapDog2"
    
    # Find all C# files with LoggerMessage
    cs_files = []
    for cs_file in snapdog_dir.rglob("*.cs"):
        if "obj" in str(cs_file) or "Tests" in str(cs_file):
            continue
        try:
            if "LoggerMessage" in cs_file.read_text(encoding='utf-8'):
                cs_files.append(cs_file)
        except:
            continue
    
    print_colored(f"📊 Found {len(cs_files)} files with LoggerMessage", 'yellow')
    
    # Categorize files and assign file indices
    file_assignments = {}
    category_counters = defaultdict(int)
    
    for cs_file in sorted(cs_files):  # Sort for deterministic order
        category = categorize_file(cs_file, snapdog_dir)
        file_index = category_counters[category]
        category_counters[category] += 1
        
        base_id = get_category_base(category)
        file_base = base_id + (file_index * 100)
        
        file_assignments[cs_file] = {
            'category': category,
            'file_index': file_index,
            'file_base': file_base
        }
    
    print_colored("\n📋 File assignments:", 'blue')
    for category, count in category_counters.items():
        base = get_category_base(category)
        print(f"  {category}: {count} files ({base}-{base + count * 100 - 1})")
    
    # Process each file and reassign EventIds
    total_changes = 0
    
    for cs_file, assignment in file_assignments.items():
        try:
            content = cs_file.read_text(encoding='utf-8')
            lines = content.split('\n')
            
            # Find all EventIds in this file
            eventids_in_file = []
            for line_num, line in enumerate(lines):
                match = re.search(r'EventId\s*=\s*(\d+)', line)
                if match:
                    eventids_in_file.append((line_num, int(match.group(1))))
            
            if not eventids_in_file:
                continue
            
            # Calculate new EventIds for this file
            file_base = assignment['file_base']
            new_eventids = {old_id: file_base + i for i, (_, old_id) in enumerate(eventids_in_file)}
            
            # Check if any changes needed
            changes_needed = any(old_id != new_id for old_id, new_id in new_eventids.items())
            
            if changes_needed:
                # Apply changes
                for line_num, old_id in eventids_in_file:
                    new_id = new_eventids[old_id]
                    old_line = lines[line_num]
                    new_line = re.sub(rf'(EventId\s*=\s*){old_id}\b', rf'\g<1>{new_id}', old_line)
                    lines[line_num] = new_line
                
                # Write back
                cs_file.write_text('\n'.join(lines), encoding='utf-8')
                
                relative_path = cs_file.relative_to(snapdog_dir)
                category = assignment['category']
                print_colored(f"📄 {relative_path} ({category}): {len(eventids_in_file)} EventIds", 'green')
                
                for old_id, new_id in new_eventids.items():
                    if old_id != new_id:
                        print(f"   {old_id} → {new_id}")
                
                total_changes += len([1 for old_id, new_id in new_eventids.items() if old_id != new_id])
        
        except Exception as e:
            relative_path = cs_file.relative_to(snapdog_dir)
            print_colored(f"❌ Error processing {relative_path}: {e}", 'red')
    
    if total_changes == 0:
        print_colored("\n✅ All EventIds already properly organized!", 'green')
    else:
        print_colored(f"\n✅ Reorganized {total_changes} EventIds across {len([f for f, a in file_assignments.items() if any(cs_file == f for cs_file in cs_files)])} files", 'green')
    
    # Verify no duplicates
    print_colored("\n🔍 Verifying uniqueness...", 'blue')
    all_eventids = set()
    duplicates = []
    
    for cs_file in cs_files:
        try:
            content = cs_file.read_text(encoding='utf-8')
            for match in re.finditer(r'EventId\s*=\s*(\d+)', content):
                eventid = int(match.group(1))
                if eventid in all_eventids:
                    duplicates.append(eventid)
                all_eventids.add(eventid)
        except:
            continue
    
    if duplicates:
        print_colored(f"❌ Found {len(duplicates)} duplicates!", 'red')
        return 1
    else:
        print_colored(f"✅ All {len(all_eventids)} EventIds are unique!", 'green')
        return 0

if __name__ == "__main__":
    exit(main())
