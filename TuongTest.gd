extends Node3D

# --- KHAI BÁO BIẾN ---
@onready var camera = $Camera3D
@onready var node_units = $BanCo # Hoặc tạo 1 Node3D con để chứa lính cho gọn
@onready var hang_cho = $HangCho # Nơi chứa lính dự bị

# Link tới mẫu con lính (Preload file .tscn lính của bạn vào đây)
var unit_scene = preload("res://Tuong_Test.tscn") 

var selected_unit: Node3D = null # Lưu con lính đang được chọn
var dragging: bool = false # Kiểm tra xem có đang thao tác không

# --- KẾT NỐI UI (NÚT MUA) ---
func _ready():
	# Kết nối nút mua lính từ UI (Đảm bảo đường dẫn đúng theo ảnh của bạn)
	$UI/NutMuaLinh.pressed.connect(_on_nut_mua_linh_pressed)

# --- HÀM 1: MUA LÍNH (SPAWN) ---
func _on_nut_mua_linh_pressed():
	# Tìm một slot trống trong Hàng Chờ hoặc Bàn Cờ để đặt
	var empty_slot = find_empty_slot()
	
	if empty_slot:
		spawn_unit(empty_slot)
	else:
		print("Không còn chỗ trống để mua lính!")

func spawn_unit(slot_target):
	var new_unit = unit_scene.instantiate()
	node_units.add_child(new_unit) # Thêm vào Scene
	
	# Đặt vị trí lính ngay tại Slot
	new_unit.global_position = slot_target.global_position
	
	# Gắn thông tin: Lính này đang thuộc về Slot nào
	new_unit.set_meta("current_slot", slot_target)
	slot_target.set_meta("has_unit", new_unit) # Slot biết nó đang chứa ai

# Hàm phụ: Tìm slot trống (Quét qua các con của HangCho và BanCo)
func find_empty_slot():
	# Ưu tiên tìm trong Hàng Chờ trước
	for slot in hang_cho.get_children():
		if slot.is_in_group("Slots") and not slot.has_meta("has_unit"):
			return slot
	
	# Nếu hàng chờ đầy, tìm trên Bàn Cờ (Tùy logic game bạn)
	for slot in $BanCo.get_children():
		if slot.is_in_group("Slots") and not slot.has_meta("has_unit"):
			return slot
	return null

# --- HÀM 2: XỬ LÝ CLICK CHUỘT (RAYCAST) ---
func _unhandled_input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var result = raycast_from_mouse(event.position)
		
		if result:
			var collider = result.collider
			handle_click_object(collider)

# Bắn tia từ Camera để xem chuột click vào cái gì
func raycast_from_mouse(mouse_pos):
	var space_state = get_world_3d().direct_space_state
	var origin = camera.project_ray_origin(mouse_pos)
	var end = origin + camera.project_ray_normal(mouse_pos) * 1000
	
	# Tạo query bắn tia
	var query = PhysicsRayQueryParameters3D.create(origin, end)
	query.collide_with_areas = true # Nếu Slot/Lính dùng Area3D
	query.collide_with_bodies = true # Nếu Slot/Lính dùng StaticBody/CharacterBody
	
	return space_state.intersect_ray(query)

# --- HÀM 3: LOGIC CHỌN VÀ DI CHUYỂN ---
func handle_click_object(obj):
	# TRƯỜNG HỢP 1: Click vào LÍNH -> Chọn lính đó
	# (Lưu ý: Nếu lính là CharacterBody, obj sẽ là chính nó. Nếu có nhiều part con, cần check parent)
	var unit_root = obj
	while unit_root and not unit_root.is_in_group("Units") and unit_root != self:
		unit_root = unit_root.get_parent()
		
	if unit_root and unit_root.is_in_group("Units"):
		selected_unit = unit_root
		print("Đã chọn lính: ", selected_unit.name)
		# Có thể thêm code đổi màu lính để biết đang chọn
		return

	# TRƯỜNG HỢP 2: Click vào SLOT ĐẤT -> Di chuyển hoặc Đổi chỗ
	if obj.is_in_group("Slots") and selected_unit != null:
		var target_slot = obj
		var old_slot = selected_unit.get_meta("current_slot")
		
		# Kiểm tra xem Slot đích có lính chưa
		if target_slot.has_meta("has_unit"):
			# -- LOGIC ĐỔI CHỖ (SWAP) --
			var unit_at_target = target_slot.get_meta("has_unit")
			
			if unit_at_target != selected_unit:
				# 1. Đưa con ở đích về slot cũ
				unit_at_target.global_position = old_slot.global_position
				unit_at_target.set_meta("current_slot", old_slot)
				old_slot.set_meta("has_unit", unit_at_target)
				
				# 2. Đưa con đang chọn đến slot đích
				selected_unit.global_position = target_slot.global_position
				selected_unit.set_meta("current_slot", target_slot)
				target_slot.set_meta("has_unit", selected_unit)
				
				print("Đã đổi chỗ 2 lính")
				selected_unit = null # Bỏ chọn
		else:
			# -- LOGIC DI CHUYỂN VÀO Ô TRỐNG --
			# Xóa thông tin ở slot cũ
			old_slot.remove_meta("has_unit")
			
			# Cập nhật vị trí mới
			selected_unit.global_position = target_slot.global_position
			selected_unit.set_meta("current_slot", target_slot)
			target_slot.set_meta("has_unit", selected_unit)
			
			print("Đã di chuyển lính")
			selected_unit = null # Bỏ chọn
