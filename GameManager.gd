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
	# 1. Kết nối nút Mua Lính
	if has_node("UI/NutMuaLinh"):
		$UI/NutMuaLinh.pressed.connect(_khi_bam_mua_linh)
	
	# 2. Kết nối nút Bắt Đầu
	if has_node("UI/NutBatDau"):
		$UI/NutBatDau.pressed.connect(_khi_bam_bat_dau)
	
	# 3. Sinh quái Wave 1
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
	# 1. TÌM NODE GỐC CỦA LÍNH (nếu click vào tay chân, vũ khí...)
	var unit_check = obj
	var is_unit = false
	while unit_check and unit_check != self:
		if unit_check.has_meta("current_slot"):
			is_unit = true
			break
		unit_check = unit_check.get_parent()
	
	# --- TRƯỜNG HỢP 1: CLICK VÀO LÍNH ---
	if is_unit:
		# Chỉ tương tác nếu là lính phe mình
		if unit_check.is_in_group("DongMinh"):
			
			# A. Nếu CHƯA chọn ai cả -> Thì chọn con này
			if unit_dang_chon == null:
				unit_dang_chon = unit_check
				print("👉 Đã chọn: ", unit_dang_chon.name)
				
			# B. Nếu ĐANG chọn 1 con khác -> Thì đổi chỗ với con này
			elif unit_dang_chon != unit_check:
				print("🔄 Phát hiện lính khác -> Thực hiện đổi chỗ")
				
				# Lấy cái Slot mà con lính kia đang đứng
				var slot_cua_linh_kia = unit_check.get_meta("current_slot")
				
				# Gọi hàm di chuyển vào cái Slot đó (Hàm di chuyển sẽ tự lo vụ đổi chỗ)
				di_chuyen_linh(unit_dang_chon, slot_cua_linh_kia)
				
				# Đổi xong thì bỏ chọn
				unit_dang_chon = null
				
			# C. Nếu click lại vào chính con đang chọn -> Bỏ chọn
			else:
				print("⏹️ Bỏ chọn")
				unit_dang_chon = null
		return

	# --- TRƯỜNG HỢP 2: CLICK VÀO Ô ĐẤT TRỐNG ---
	var slot_check = obj
	if not (slot_check.name.begins_with("Slot") or slot_check.name.begins_with("Tile")):
		slot_check = slot_check.get_parent()
	
	if slot_check.name.begins_with("Slot") or slot_check.name.begins_with("Tile"):
		if unit_dang_chon != null:
			di_chuyen_linh(unit_dang_chon, slot_check)
			unit_dang_chon = null # Bỏ chọn sau khi di chuyển
func di_chuyen_linh(unit, target_slot):
	var old_slot = unit.get_meta("current_slot")
	
	# Nếu ô đích đã có lính -> Đổi chỗ
	if target_slot.has_meta("has_unit"):
		var unit_tai_dich = target_slot.get_meta("has_unit")
		if unit_tai_dich != unit:
			print("🔄 Hoán đổi vị trí!")
			teleport_to_slot(unit_tai_dich, old_slot)
			teleport_to_slot(unit, target_slot)
	else:
		# Nếu ô đích trống -> Di chuyển
		print("✅ Di chuyển tới ô trống")
		old_slot.remove_meta("has_unit")
		teleport_to_slot(unit, target_slot)

func teleport_to_slot(unit, slot):
	var vi_tri_moi = slot.global_position
	
	unit.global_position = slot.global_position
	unit.set_meta("current_slot", slot)
	slot.set_meta("has_unit", unit)
	
	if "tren_san_dau" in unit:
		if slot.name.begins_with("Tile"):
			unit.tren_san_dau = true 
		else:
			unit.tren_san_dau = false

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
	# Ưu tiên tìm hàng chờ (Slot_)
	for slot in node_hang_cho.get_children():
		if slot.name.begins_with("Slot") and not slot.has_meta("has_unit"):
			return slot
	# Hết chỗ thì tìm bàn cờ (Tile_)
	for slot in node_ban_co.get_children():
		if slot.name.begins_with("Tile") and not slot.has_meta("has_unit"):
			return slot
	return null

func sinh_linh_moi(slot):
	var linh = mau_tuong.instantiate()
	node_ban_co.add_child(linh) # Thêm vào cây
	linh.add_to_group("DongMinh")
	teleport_to_slot(linh, slot)

# ==========================================
# PHẦN 3: LOGIC WAVE & GAMEPLAY
# ==========================================
func _khi_bam_bat_dau():
	var so_luong_quai = get_tree().get_nodes_in_group("KeThu").size()
	
	if so_luong_quai > 0:
		print("⚔️ VÀO TRẬN CHIẾN (Wave ", wave_hien_tai, ")")
		# Ẩn nút UI
		if has_node("UI/NutMuaLinh"): $UI/NutMuaLinh.visible = false
		if has_node("UI/NutBatDau"): $UI/NutBatDau.visible = false
		
		# Kích hoạt AI đánh nhau
		get_tree().call_group("DongMinh", "vao_tran")
		get_tree().call_group("KeThu", "vao_tran")
	else:
		print("🏆 Chiến thắng! Sang vòng sau...")
		wave_hien_tai += 1
		if has_node("UI/NutMuaLinh"): $UI/NutMuaLinh.visible = true
		tao_wave_quai(wave_hien_tai)

func tao_wave_quai(level):
	print("🐺 Triệu hồi quái Wave: ", level)
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
		add_child(quai)
		quai.add_to_group("KeThu") 
		quai.global_position = o_dich.global_position
		
		# Gán biến để AI nhận diện (Đã có kiểm tra an toàn)
		if "tren_san_dau" in quai: quai.tren_san_dau = true
