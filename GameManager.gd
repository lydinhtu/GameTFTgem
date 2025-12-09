extends Node3D

# --- KHAI BÁO CÁC MẪU ---
var mau_tuong = preload("res://Tuong_Test.tscn")
var mau_quai = preload("res://Enemy.tscn") 

var tien_vang = 100
var unit_dang_chon = null 
var wave_hien_tai = 1 

func _ready():
	# 1. Kết nối nút bấm
	var nut_mua = get_node_or_null("UI/NutMuaLinh")
	if nut_mua: nut_mua.pressed.connect(_khi_bam_mua_linh)
	
	var nut_bat_dau = get_node_or_null("UI/NutBatDau")
	if nut_bat_dau: nut_bat_dau.pressed.connect(_khi_bam_bat_dau)
	
	# 2. Đợi kết nối ô đất
	await get_tree().create_timer(0.5).timeout
	ket_noi_cac_o()
	
	# 3. Sinh quái Wave 1 ngay khi vào game
	tao_wave_quai(wave_hien_tai)

# --- XỬ LÝ NÚT BẮT ĐẦU / QUA MÀN ---
func _khi_bam_bat_dau():
	var so_luong_quai = get_tree().get_nodes_in_group("KeThu").size()
	
	if so_luong_quai > 0:
		print("⚔️ VÀO TRẬN CHIẾN (Wave ", wave_hien_tai, ")")
		
		# Ẩn nút đi
		var ui_mua = get_node_or_null("UI/NutMuaLinh")
		if ui_mua: ui_mua.visible = false
		var ui_start = get_node_or_null("UI/NutBatDau")
		if ui_start: ui_start.visible = false
		
		# Hô hào đánh nhau
		get_tree().call_group("DongMinh", "vao_tran")
		get_tree().call_group("KeThu", "vao_tran")
		
	else:
		print("🏆 Sang vòng tiếp theo...")
		wave_hien_tai += 1
		
		# Hiện lại nút mua
		var ui_mua = get_node_or_null("UI/NutMuaLinh")
		if ui_mua: ui_mua.visible = true
		
		tao_wave_quai(wave_hien_tai)

# --- HỆ THỐNG SINH QUÁI ---
func tao_wave_quai(level):
	print("🐺 Đang triệu hồi quái Wave: ", level)
	
	if level == 1:
		sinh_quai_tai_o("Tile_4_7") 
	elif level == 2:
		sinh_quai_tai_o("Tile_3_7")
		sinh_quai_tai_o("Tile_5_7")
	elif level == 3:
		sinh_quai_tai_o("Tile_3_7")
		sinh_quai_tai_o("Tile_4_7")
		sinh_quai_tai_o("Tile_5_7")
	else:
		sinh_quai_tai_o("Tile_4_8")

func sinh_quai_tai_o(ten_o_dat):
	var ban_co = get_node_or_null("BanCo")
	if not ban_co: return
	var o_dich = ban_co.get_node_or_null(ten_o_dat)
	
	if o_dich:
		var quai = mau_quai.instantiate()
		add_child(quai)
		quai.add_to_group("KeThu") 
		quai.rotation_degrees.y = 180 
		quai.global_position = o_dich.global_position + Vector3(0, 1.5, 0)
		
		# [FIX QUAN TRỌNG] Báo cho quái biết là nó đang đứng trên sân
		# Thêm dòng này vào thì lính mới nhìn thấy quái để đánh!
		if "tren_san_dau" in quai:
			quai.tren_san_dau = true 

		print("👹 Quái xuất hiện tại: ", ten_o_dat)

# --- MUA LÍNH ---
func _khi_bam_mua_linh():
	if tien_vang < 10: return
	var hang_cho = get_node("HangCho")
	var slot_tim_duoc = null
	
	for slot in hang_cho.get_children():
		if "Enemy" in slot.name or not slot.name.begins_with("Slot"): continue
		if tim_tuong_tai_vi_tri(slot.global_position) == null:
			slot_tim_duoc = slot
			break 
	
	if slot_tim_duoc:
		tien_vang -= 10
		sinh_linh(slot_tim_duoc) 

func sinh_linh(slot_dich):
	var linh = mau_tuong.instantiate()
	add_child(linh)
	linh.add_to_group("DongMinh")
	# Gửi luôn node slot để lính biết nó đang ở hàng chờ (xoay mặt 0 độ)
	linh.di_chuyen_den(slot_dich) 
	linh.input_ray_pickable = true

# --- LOGIC CHỌN & DI CHUYỂN ---
func chon_tuong(u_moi):
	if u_moi.dang_chien_dau: return
	if unit_dang_chon == null:
		unit_dang_chon = u_moi
		print("👉 Đã chọn: ", u_moi.name)
		return
	if unit_dang_chon == u_moi:
		unit_dang_chon = null
		print("⏹️ Bỏ chọn")
		return
	if unit_dang_chon != u_moi:
		thuc_hien_hoan_doi(unit_dang_chon, u_moi)
		unit_dang_chon = null

# [QUAN TRỌNG] Hàm này đã sửa để gửi NODE ĐẤT thay vì vị trí
func _khi_click_vao_o(cam, ev, pos, nor, idx, o_dat):
	if ev is InputEventMouseButton and ev.button_index == MOUSE_BUTTON_LEFT and ev.pressed:
		
		if unit_dang_chon:
			var tuong_o_dich = tim_tuong_tai_vi_tri(o_dat.global_position, unit_dang_chon)
			
			if tuong_o_dich != null:
				thuc_hien_hoan_doi(unit_dang_chon, tuong_o_dich)
			else:
				# Gửi nguyên cái Node ô đất đi
				unit_dang_chon.di_chuyen_den(o_dat)
			
			unit_dang_chon = null 

func thuc_hien_hoan_doi(unit_1, unit_2):
	var slot_1 = tim_slot_duoi_chan(unit_1.global_position)
	var slot_2 = tim_slot_duoi_chan(unit_2.global_position)
	if slot_1 and slot_2:
		unit_1.di_chuyen_den(slot_2)
		unit_2.di_chuyen_den(slot_1)

# --- HÀM PHỤ TRỢ ---
func ket_noi_cac_o():
	var tat_ca_cac_o = []
	if has_node("BanCo"): tat_ca_cac_o.append_array(get_node("BanCo").get_children())
	if has_node("HangCho"): tat_ca_cac_o.append_array(get_node("HangCho").get_children())
	
	for o in tat_ca_cac_o:
		var body = o.get_node_or_null("StaticBody3D")
		if not body: body = o.get_node_or_null("Slot_1/StaticBody3D")
		if body:
			if body.input_event.is_connected(_khi_click_vao_o):
				body.input_event.disconnect(_khi_click_vao_o)
			body.input_event.connect(_khi_click_vao_o.bind(o))
	print("✅ Đã kết nối xong các ô!")

func tim_tuong_tai_vi_tri(vi_tri_check, tuong_bo_qua = null):
	var ds_tuong = get_tree().get_nodes_in_group("DongMinh")
	for tuong in ds_tuong:
		if tuong == tuong_bo_qua: continue
		var p1 = Vector2(tuong.global_position.x, tuong.global_position.z)
		var p2 = Vector2(vi_tri_check.x, vi_tri_check.z)
		if p1.distance_to(p2) < 0.6: return tuong 
	return null

func tim_slot_duoi_chan(vi_tri_tuong):
	var ds_slot = []
	if has_node("BanCo"): ds_slot.append_array(get_node("BanCo").get_children())
	if has_node("HangCho"): ds_slot.append_array(get_node("HangCho").get_children())
	for slot in ds_slot:
		var p1 = Vector2(slot.global_position.x, slot.global_position.z)
		var p2 = Vector2(vi_tri_tuong.x, vi_tri_tuong.z)
		if p1.distance_to(p2) < 0.6: return slot
	return null
