extends Node3D

# --- CẤU HÌNH ---
var mau_tuong = preload("res://Tuong_Test.tscn")
var mau_quai = preload("res://Enemy.tscn") 

var tien_vang = 100
var wave_hien_tai = 1 
var unit_dang_chon = null # Lưu con lính đang chọn

# Node tham chiếu
@onready var cam = $Camera3D
@onready var node_hang_cho = $HangCho
@onready var node_ban_co = $BanCo

func _ready():
	# 1. Kết nối nút
	if has_node("UI/NutMuaLinh"):
		$UI/NutMuaLinh.pressed.connect(_khi_bam_mua_linh)
	if has_node("UI/NutBatDau"):
		$UI/NutBatDau.pressed.connect(_khi_bam_bat_dau)
	
	# 2. Sinh quái Wave 1
	tao_wave_quai(wave_hien_tai)
	print("🎮 Game đã sẵn sàng! Vàng: ", tien_vang)

# ==========================================
# PHẦN 1: XỬ LÝ CLICK & DI CHUYỂN
# ==========================================
func _unhandled_input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var result = ban_tia_raycast(event.position)
		if result:
			xu_ly_click(result.collider)

func ban_tia_raycast(mouse_pos):
	var space_state = get_world_3d().direct_space_state
	var origin = cam.project_ray_origin(mouse_pos)
	var end = origin + cam.project_ray_normal(mouse_pos) * 1000
	var query = PhysicsRayQueryParameters3D.create(origin, end)
	query.collide_with_areas = true
	query.collide_with_bodies = true
	return space_state.intersect_ray(query)

func xu_ly_click(obj):
	# 1. TÌM NODE GỐC CỦA LÍNH
	var unit_check = obj
	var is_unit = false
	while unit_check and unit_check != self:
		if unit_check.has_meta("current_slot"):
			is_unit = true
			break
		unit_check = unit_check.get_parent()
	
	# --- TRƯỜNG HỢP 1: CLICK VÀO LÍNH (CHỌN/ĐỔI CHỖ) ---
	if is_unit:
		if unit_check.is_in_group("DongMinh"):
			if unit_dang_chon == null:
				unit_dang_chon = unit_check
				print("👉 Đã chọn: ", unit_dang_chon.name)
			elif unit_dang_chon != unit_check:
				print("🔄 Thực hiện đổi chỗ")
				var slot_cua_linh_kia = unit_check.get_meta("current_slot")
				di_chuyen_linh(unit_dang_chon, slot_cua_linh_kia)
				unit_dang_chon = null
			else:
				print("⏹️ Bỏ chọn")
				unit_dang_chon = null
		return

	# --- TRƯỜNG HỢP 2: CLICK VÀO Ô ĐẤT (DI CHUYỂN) ---
	var slot_check = obj
	if not (slot_check.name.begins_with("Slot") or slot_check.name.begins_with("Tile")):
		slot_check = slot_check.get_parent()
	
	if slot_check.name.begins_with("Slot") or slot_check.name.begins_with("Tile"):
		if unit_dang_chon != null:
			di_chuyen_linh(unit_dang_chon, slot_check)
			unit_dang_chon = null 

func di_chuyen_linh(unit, target_slot):
	var old_slot = unit.get_meta("current_slot")
	
	if target_slot.has_meta("has_unit"):
		var unit_tai_dich = target_slot.get_meta("has_unit")
		if unit_tai_dich != unit:
			teleport_to_slot(unit_tai_dich, old_slot)
			teleport_to_slot(unit, target_slot)
	else:
		old_slot.remove_meta("has_unit")
		teleport_to_slot(unit, target_slot)

# [HÀM QUAN TRỌNG: BẮT DÍNH VÀO TÂM Ô - GRID SNAPPING]
func teleport_to_slot(unit, slot):
	var vi_tri_slot = slot.global_position
	var is_on_tile = slot.name.begins_with("Tile") 
	
	var vi_tri_moi = vi_tri_slot
	
	# 1. Áp dụng Grid Snapping cho X và Z
	# Công thức: round(tọa độ / kích thước ô) * kích thước ô. (Kích thước ô là 2.0m)
	var x_grid = round(vi_tri_slot.x / 2.0) * 2.0
	var z_grid = round(vi_tri_slot.z / 2.0) * 2.0
	
	vi_tri_moi.x = x_grid
	vi_tri_moi.z = z_grid
	
	# 2. Chống lún: Nâng cao Y lên 0.5m
	vi_tri_moi.y = vi_tri_slot.y + 0.5 

	# 3. Gán vị trí và Metadata
	unit.global_position = vi_tri_moi
	unit.set_meta("current_slot", slot)
	slot.set_meta("has_unit", unit)
	
	# 4. Cập nhật trạng thái chiến đấu
	if "tren_san_dau" in unit:
		unit.tren_san_dau = is_on_tile
		
	# 5. Chỉnh hướng mặt (Tùy thuộc lính/quái và vị trí)
	if unit.is_in_group("DongMinh"):
		unit.rotation_degrees.y = 0 if not is_on_tile else 180
	elif unit.is_in_group("KeThu"):
		unit.rotation_degrees.y = 0 if not is_on_tile else 180 

# ==========================================
# PHẦN 2: MUA LÍNH & TÀI NGUYÊN
# ==========================================
func _khi_bam_mua_linh():
	if tien_vang < 10:
		print("❌ Không đủ tiền! Cần 10 vàng.")	
		return
		
	var cho_trong = tim_cho_trong_de_mua()
	if cho_trong:
		tien_vang -= 10
		print("💰 Đã mua lính. Vàng còn: ", tien_vang)
		sinh_linh_moi(cho_trong)
	else:
		print("⚠️ Hàng chờ và Bàn cờ đều đã đầy!")

func tim_cho_trong_de_mua():
	for slot in node_hang_cho.get_children():
		if slot.name.begins_with("Slot") and not slot.has_meta("has_unit"):
			return slot
	for slot in node_ban_co.get_children():
		if slot.name.begins_with("Tile") and not slot.has_meta("has_unit"):
			return slot
	return null

func sinh_linh_moi(slot):
	var linh = mau_tuong.instantiate()
	# Gán vị trí tạm thời trước khi teleport (giúp logic snap hoạt động)
	linh.global_position = slot.global_position 
	linh.add_to_group("DongMinh")
	node_ban_co.add_child(linh)
	teleport_to_slot(linh, slot)

# ==========================================
# PHẦN 3: LOGIC WAVE & GAMEPLAY
# ==========================================
func _khi_bam_bat_dau():
	var so_luong_quai = get_tree().get_nodes_in_group("KeThu").size()
	
	if so_luong_quai > 0:
		print("⚔️ VÀO TRẬN CHIẾN (Wave ", wave_hien_tai, ")")
		
		if has_node("UI/NutMuaLinh"): $UI/NutMuaLinh.visible = false
		if has_node("UI/NutBatDau"): $UI/NutBatDau.visible = false
		
		get_tree().call_group("DongMinh", "vao_tran")
		get_tree().call_group("KeThu", "vao_tran")
	else:
		print("🏆 Chiến thắng! Sang vòng sau...")
		wave_hien_tai += 1
		if has_node("UI/NutMuaLinh"): $UI/NutMuaLinh.visible = true
		# [FIX LỖI TYPO] Đã sửa wave_hien_ai thành wave_hien_tai
		tao_wave_quai(wave_hien_tai) 

func tao_wave_quai(level):
	if level == 1:
		sinh_quai("Tile_4_7") 
	elif level == 2:
		sinh_quai("Tile_3_7")
		sinh_quai("Tile_5_7")
	else:
		sinh_quai("Tile_4_7")
		sinh_quai("Tile_3_7")
		sinh_quai("Tile_5_7")

func sinh_quai(ten_o_dat):
	var o_dich = node_ban_co.get_node_or_null(ten_o_dat)
	if o_dich:
		var quai = mau_quai.instantiate()
		
		# Gán vị trí tạm thời
		quai.global_position = o_dich.global_position 
		
		quai.add_to_group("KeThu") 
		add_child(quai)
		teleport_to_slot(quai, o_dich) # Dùng teleport_to_slot để snap vị trí

		if "tren_san_dau" in quai: quai.tren_san_dau = true
